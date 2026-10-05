import { FormControl } from '@angular/forms';

import { calendarToday, cedisAmount, formatMoney, rateInForce } from './rate-schedule';

describe('rate schedule', () => {
  const open = {
    id: 'open',
    amount: 10,
    effectiveFrom: '2026-01-01',
    effectiveTo: null,
  };
  const closed = {
    id: 'closed',
    amount: 99,
    effectiveFrom: '2024-01-01',
    effectiveTo: '2024-06-01',
  };

  it('treats a missing row as a gap and keeps an open-ended amount in force', () => {
    expect(rateInForce([closed], '2026-03-01')).toBeNull();
    expect(rateInForce([open, closed], '2026-03-01')?.id).toBe('open');
  });

  it('excludes the end date', () => {
    const bounded = { ...open, effectiveTo: '2026-06-01' };

    expect(rateInForce([bounded], '2026-05-31')?.id).toBe('open');
    expect(rateInForce([bounded], '2026-06-01')).toBeNull();
  });

  it('formats cedis with the tenant code and two decimals', () => {
    expect(formatMoney(1250, 'GHS')).toBe('GHS 1,250.00');
    expect(formatMoney(10.5, 'GHS')).toBe('GHS 10.50');
  });

  it('accepts zero and rejects a third decimal place', () => {
    expect(cedisAmount(new FormControl('0'))).toBeNull();
    expect(cedisAmount(new FormControl('10.555'))).toEqual({ amount: true });
    expect(cedisAmount(new FormControl(''))).toEqual({ required: true });
  });

  it('returns a calendar day for the tenant zone', () => {
    const day = calendarToday('Africa/Accra', new Date('2026-10-05T00:30:00Z'));

    expect(day).toBe('2026-10-05');
  });
});
