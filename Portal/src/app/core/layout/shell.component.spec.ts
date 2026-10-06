import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { MeResponse } from '../auth/auth.models';
import { AuthService } from '../auth/auth.service';
import { ShellComponent } from './shell.component';

describe('ShellComponent', () => {
  const profile = (role: MeResponse['role']): MeResponse => ({
    tenantId: 'tenant',
    tenantName: 'Development University',
    userId: 'user',
    email: 'person@claimbase.test',
    displayName: 'Person',
    role,
    currencyCode: 'GHS',
    timeZoneId: 'Africa/Accra',
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideRouter([
          { path: 'app', children: [] },
          { path: 'app/campuses', children: [] },
          { path: 'app/faculties', children: [] },
          { path: 'app/semesters', children: [] },
          { path: 'app/courses', children: [] },
          { path: 'app/staff', children: [] },
          { path: 'app/rates', children: [] },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
  });

  it('shows academic setup to an admin and hides it from finance', () => {
    const auth = TestBed.inject(AuthService);
    auth.profile.set(profile('Admin'));
    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Campuses');
    expect(fixture.nativeElement.textContent).toContain('Staff');
    expect(fixture.nativeElement.textContent).toContain('Rates');

    auth.profile.set(profile('Finance'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Campuses');
    expect(fixture.nativeElement.textContent).toContain('Home');
  });

  it('opens the phone drawer from the menu and closes it with Escape', async () => {
    const queries: string[] = [];
    window.matchMedia = ((query: string) => {
      queries.push(query);
      return {
        matches: false,
        media: query,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        dispatchEvent: () => false,
        onchange: null,
        addListener: () => undefined,
        removeListener: () => undefined,
      };
    }) as unknown as typeof window.matchMedia;

    const auth = TestBed.inject(AuthService);
    auth.profile.set(profile('Admin'));
    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    const menu = fixture.nativeElement.querySelector('[aria-controls="portal-nav"]') as HTMLButtonElement;
    const nav = fixture.nativeElement.querySelector('#portal-nav') as HTMLElement;
    const header = fixture.nativeElement.querySelector('header') as HTMLElement;
    const main = fixture.nativeElement.querySelector('main') as HTMLElement;
    expect(queries).toContain('(min-width: 64rem)');
    expect(menu.getAttribute('aria-expanded')).toBe('false');
    expect(nav.hasAttribute('inert')).toBe(true);
    expect(header.hasAttribute('inert')).toBe(false);
    expect(main.hasAttribute('inert')).toBe(false);

    menu.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(menu.getAttribute('aria-expanded')).toBe('true');
    expect(nav.getAttribute('role')).toBe('dialog');
    expect(nav.hasAttribute('inert')).toBe(false);
    expect(header.hasAttribute('inert')).toBe(true);
    expect(main.hasAttribute('inert')).toBe(true);
    expect(document.activeElement).toBe(fixture.nativeElement.querySelector('#portal-nav-close'));

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();

    expect(menu.getAttribute('aria-expanded')).toBe('false');
    expect(nav.hasAttribute('inert')).toBe(true);
    expect(header.hasAttribute('inert')).toBe(false);
    expect(main.hasAttribute('inert')).toBe(false);
    fixture.destroy();
  });
});
