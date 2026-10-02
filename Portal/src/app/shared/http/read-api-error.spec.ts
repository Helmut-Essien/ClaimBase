import { HttpErrorResponse } from '@angular/common/http';

import { readApiError } from './read-api-error';

describe('readApiError', () => {
  it('uses the API message for a conflict', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: { message: 'Those position dates overlap another appointment.' },
    });

    expect(readApiError(error, 'fallback')).toBe('Those position dates overlap another appointment.');
  });

  it('joins validation messages', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: [{ property: 'Name', message: 'Name is required.' }] },
    });

    expect(readApiError(error, 'fallback')).toBe('Name is required.');
  });

  it('falls back when the body has no message', () => {
    const error = new HttpErrorResponse({ status: 500, error: {} });

    expect(readApiError(error, 'Something went wrong.')).toBe('Something went wrong.');
  });
});
