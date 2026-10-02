import { HttpErrorResponse } from '@angular/common/http';

/**
 * Reads the API error body written by `ExceptionHandlingMiddleware`.
 * Conflict and validation failures stay on the form as this text.
 */
export function readApiError(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  const body = error.error;
  if (typeof body === 'string' && body.trim().length > 0) {
    return body;
  }

  if (!body || typeof body !== 'object') {
    return fallback;
  }

  const record = body as { message?: unknown; errors?: unknown };
  if (typeof record.message === 'string' && record.message.trim().length > 0) {
    return record.message;
  }

  if (!Array.isArray(record.errors)) {
    return fallback;
  }

  const messages = record.errors
    .map((item) => {
      if (!item || typeof item !== 'object' || !('message' in item)) {
        return '';
      }
      const message = (item as { message?: unknown }).message;
      return typeof message === 'string' ? message.trim() : '';
    })
    .filter((message) => message.length > 0);

  return messages.length > 0 ? messages.join(' ') : fallback;
}
