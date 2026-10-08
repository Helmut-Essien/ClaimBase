/** Browser key for the last email typed on sign-in. The password is never stored. */
export const LOGIN_DRAFT_KEY = 'claimbase.login-draft';

/** Last email saved by sign-in. Does not write storage. Forgot password prefills from this. */
export function readLoginDraftEmail(): string {
  try {
    const raw = localStorage.getItem(LOGIN_DRAFT_KEY);
    if (!raw) {
      return '';
    }
    const parsed = JSON.parse(raw) as { email?: unknown };
    return typeof parsed.email === 'string' ? parsed.email : '';
  } catch {
    return '';
  }
}
