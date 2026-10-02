/**
 * Formats an API calendar date (`yyyy-MM-dd`) without passing it through local midnight.
 * Semester days and appointment days are dates, not instants.
 */
export function formatCalendarDate(value: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  if (!match) {
    return value;
  }

  const utc = new Date(Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3])));
  return new Intl.DateTimeFormat('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(utc);
}

/**
 * Short zone name for the once-per-page caption, such as GMT for Africa/Accra.
 * Falls back to the IANA id when the zone cannot be resolved.
 */
export function zoneAbbreviation(timeZone: string, now = new Date()): string {
  try {
    const part = new Intl.DateTimeFormat('en-GB', { timeZone, timeZoneName: 'short' })
      .formatToParts(now)
      .find((item) => item.type === 'timeZoneName');
    return part?.value ?? timeZone;
  } catch {
    return timeZone;
  }
}
