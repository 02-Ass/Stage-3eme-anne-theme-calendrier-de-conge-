import dayjs from 'dayjs';
import { describe, expect, it } from 'vitest';
import { daysBetweenInclusive, formatTenure, recalcVacation } from './dateMath';

describe('daysBetweenInclusive', () => {
  it('считает включительно (1–10 июля = 10 дней)', () => {
    expect(daysBetweenInclusive(dayjs('2026-07-01'), dayjs('2026-07-10'))).toBe(10);
  });

  it('однодневный отпуск = 1 день', () => {
    const day = dayjs('2026-07-01');
    expect(daysBetweenInclusive(day, day)).toBe(1);
  });
});

describe('recalcVacation', () => {
  it('заполнение начала и конца — считает количество дней', () => {
    const result = recalcVacation(
      { startDate: dayjs('2026-07-01'), endDate: dayjs('2026-07-10'), days: null },
      'endDate',
    );
    expect(result.days).toBe(10);
  });

  it('заполнение начала и количества дней — считает дату окончания', () => {
    const result = recalcVacation(
      { startDate: dayjs('2026-07-01'), endDate: null, days: 10 },
      'days',
    );
    expect(result.endDate?.format('YYYY-MM-DD')).toBe('2026-07-10');
  });

  it('заполнение конца и количества дней — считает дату начала', () => {
    const result = recalcVacation(
      { startDate: null, endDate: dayjs('2026-07-10'), days: 10 },
      'days',
    );
    expect(result.startDate?.format('YYYY-MM-DD')).toBe('2026-07-01');
  });

  it('ручное изменение даты начала (обе даты уже были) — пересчитывает дни', () => {
    // Было: 01–10 июля (10 дней). Пользователь подвинул начало на 05-е.
    const result = recalcVacation(
      { startDate: dayjs('2026-07-05'), endDate: dayjs('2026-07-10'), days: 10 },
      'startDate',
    );
    expect(result.days).toBe(6);
  });

  it('ручное изменение даты окончания (обе даты уже были) — пересчитывает дни', () => {
    const result = recalcVacation(
      { startDate: dayjs('2026-07-01'), endDate: dayjs('2026-07-15'), days: 10 },
      'endDate',
    );
    expect(result.days).toBe(15);
  });

  it('ручное изменение количества дней — пересчитывает дату окончания, начало остаётся якорем', () => {
    const result = recalcVacation(
      { startDate: dayjs('2026-07-01'), endDate: dayjs('2026-07-10'), days: 20 },
      'days',
    );
    expect(result.startDate?.format('YYYY-MM-DD')).toBe('2026-07-01');
    expect(result.endDate?.format('YYYY-MM-DD')).toBe('2026-07-20');
  });

  it('если новая дата окончания раньше начала — подстраивается под последнее изменение', () => {
    const result = recalcVacation(
      { startDate: dayjs('2026-07-20'), endDate: dayjs('2026-07-01'), days: null },
      'startDate',
    );
    expect(result.days).toBe(1);
    expect(result.endDate?.format('YYYY-MM-DD')).toBe('2026-07-20');
  });

  it('незаполненные поля — ничего не считает, возвращает как есть', () => {
    const draft = { startDate: dayjs('2026-07-01'), endDate: null, days: null };
    const result = recalcVacation(draft, 'startDate');
    expect(result).toEqual(draft);
  });
});

describe('formatTenure', () => {
  it('считает года и месяцы от даты устройства до сегодня', () => {
    const fiveYearsAgo = dayjs().subtract(5, 'year').subtract(3, 'month');
    const result = formatTenure(fiveYearsAgo);
    expect(result).toContain('5');
    expect(result).toMatch(/лет|года|год/);
  });

  it('меньше года — показывает только месяцы', () => {
    const twoMonthsAgo = dayjs().subtract(2, 'month');
    const result = formatTenure(twoMonthsAgo);
    expect(result).not.toMatch(/год|лет/);
    expect(result).toContain('2');
  });
});
