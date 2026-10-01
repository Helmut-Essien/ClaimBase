import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';

/** Storage key for the portal JWT. */
export const AUTH_TOKEN_KEY = 'claimbase.token';

/**
 * Holds the access token in memory and in local storage.
 * The interceptor reads this store so it does not depend on the HTTP client.
 */
@Injectable({ providedIn: 'root' })
export class AuthSession {
  private readonly browser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly clearedHandlers: Array<() => void> = [];

  /** Current token, or null when signed out. */
  readonly token = signal<string | null>(this.read());

  /**
   * Runs after {@link clear}. AuthService drops the profile here without the interceptor depending on HttpClient.
   */
  whenCleared(handler: () => void): void {
    this.clearedHandlers.push(handler);
  }

  /** Saves a portal token. Do not call this for a lecturer login. */
  store(token: string): void {
    if (this.browser) {
      localStorage.setItem(AUTH_TOKEN_KEY, token);
    }
    this.token.set(token);
  }

  /** Drops the token after sign-out, a lecturer login, or HTTP 401, then notifies listeners. */
  clear(): void {
    if (this.browser) {
      localStorage.removeItem(AUTH_TOKEN_KEY);
    }
    this.token.set(null);
    for (const handler of this.clearedHandlers) {
      handler();
    }
  }

  private read(): string | null {
    if (!this.browser) {
      return null;
    }
    return localStorage.getItem(AUTH_TOKEN_KEY);
  }
}
