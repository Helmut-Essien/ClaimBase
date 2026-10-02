import { formatCalendarDate } from './calendar-date';

describe('formatCalendarDate', () => {
  it('keeps the calendar day instead of shifting it through local midnight', () => {
    expect(formatCalendarDate('2026-01-01')).toBe('1 Jan 2026');
  });
});
