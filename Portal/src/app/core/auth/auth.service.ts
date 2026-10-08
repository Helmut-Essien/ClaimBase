import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TenantStateService } from '../tenant/tenant-state.service';
import { AuthSession } from './auth-session';
import { SESSION_PROBE } from './auth.interceptor';
import { AuthResponse, MeResponse, PasswordResetMessage } from './auth.models';

/**
 * Signs portal staff in and restores the session from `GET /api/auth/me`.
 * Lecturer accounts are rejected before the token is stored.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(AuthSession);
  private readonly tenant = inject(TenantStateService);
  private readonly router = inject(Router);

  /** Token that {@link profile} was loaded for. A different token must call `/api/auth/me` again. */
  private profileToken: string | null = null;

  /** True when the last login was a lecturer account. */
  readonly lecturerBlocked = signal(false);

  /** Profile loaded from `/api/auth/me`. */
  readonly profile = signal<MeResponse | null>(null);

  constructor() {
    this.session.whenCleared(() => this.dropProfile());
  }

  /** Current JWT, if a portal session is stored. */
  token(): string | null {
    return this.session.token();
  }

  /**
   * Posts email and password. The caller decides whether the role may stay signed in.
   */
  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, { email, password });
  }

  /**
   * Asks for a reset link. The message is the same when the address is unknown.
   * This call does not sign anyone in.
   */
  forgotPassword(email: string): Observable<PasswordResetMessage> {
    return this.http.post<PasswordResetMessage>(`${environment.apiUrl}/api/auth/forgot-password`, { email });
  }

  /**
   * Sets a new password from the emailed link. The caller signs in afterwards.
   */
  resetPassword(
    email: string,
    token: string,
    newPassword: string,
    confirmPassword: string,
  ): Observable<PasswordResetMessage> {
    return this.http.post<PasswordResetMessage>(`${environment.apiUrl}/api/auth/reset-password`, {
      email,
      token,
      newPassword,
      confirmPassword,
    });
  }

  /** Stores a non-lecturer token. A different token drops the previous profile. */
  storeToken(token: string): void {
    this.lecturerBlocked.set(false);
    if (this.profileToken !== token) {
      this.dropProfile();
    }
    this.session.store(token);
  }

  /** Clears any token and asks the lecturer to use the mobile app. */
  rejectLecturer(): void {
    this.session.clear();
    this.lecturerBlocked.set(true);
  }

  /** Clears the notice before a new attempt. */
  beginLogin(): void {
    this.lecturerBlocked.set(false);
  }

  /**
   * Loads `/api/auth/me` when a token exists.
   * Returns false for a missing token, a lecturer, or a rejected token.
   */
  async restore(): Promise<boolean> {
    const token = this.session.token();
    if (!token) {
      this.dropProfile();
      return false;
    }

    // Reuse the profile only while it still belongs to this exact token.
    if (this.profile() && this.profileToken === token) {
      return this.profile()!.role !== 'Lecturer';
    }

    try {
      const me = await firstValueFrom(
        this.http.get<MeResponse>(`${environment.apiUrl}/api/auth/me`, {
          context: new HttpContext().set(SESSION_PROBE, true),
        }),
      );
      if (this.session.token() !== token) {
        return false;
      }
      // The API also returns 403 for lecturers. This branch covers a token that was stored anyway.
      if (me.role === 'Lecturer') {
        this.rejectLecturer();
        return false;
      }
      this.profileToken = token;
      this.profile.set(me);
      this.tenant.apply(me);
      return true;
    } catch (error: unknown) {
      // A network failure or 503 must keep the token. Only an answer that the login is no longer valid ends the session.
      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 404)) {
        this.session.clear();
      } else if (error instanceof HttpErrorResponse && error.status === 403) {
        this.rejectLecturer();
      }
      return false;
    }
  }

  /** Ends the session and returns to the login screen. */
  signOut(): void {
    this.session.clear();
    this.lecturerBlocked.set(false);
    void this.router.navigateByUrl('/login');
  }

  /** Drops the signed-in profile. The token store notifies this on clear. */
  private dropProfile(): void {
    this.profileToken = null;
    this.profile.set(null);
    this.tenant.clear();
  }
}
