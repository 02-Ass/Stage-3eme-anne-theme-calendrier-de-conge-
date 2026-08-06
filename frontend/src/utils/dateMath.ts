import dayjs, { Dayjs } from 'dayjs';

export interface VacationDraft {
  startDate: Dayjs | null;
  endDate: Dayjs | null;
  days: number | null;
}

export type VacationField = 'startDate' | 'endDate' | 'days';

export function daysBetweenInclusive(start: Dayjs, end: Dayjs): number {
  return end.diff(start, 'day') + 1;
}

function endFromDays(start: Dayjs, days: number): Dayjs {
  return start.add(days - 1, 'day');
}

function startFromDays(end: Dayjs, days: number): Dayjs {
  return end.subtract(days - 1, 'day');
}

/**
 * Реализует правило ТЗ для таблицы отпусков:
 *  - заполнив любые 2 из 3 полей (дата начала, дата окончания, кол-во дней),
 *    третье поле рассчитывается автоматически;
 *  - при последующем ручном изменении даты начала ИЛИ даты окончания
 *    пересчитывается количество дней;
 *  - при ручном изменении количества дней пересчитывается дата окончания
 *    (дата начала остаётся "якорем").
 *
 * `draft` — состояние формы/строки ПОСЛЕ применения изменения пользователя,
 * `editedField` — какое из трёх полей человек только что отредактировал.
 */
export function recalcVacation(draft: VacationDraft, editedField: VacationField): VacationDraft {
  const { startDate, endDate, days } = draft;

  if (editedField === 'days') {
    if (startDate && days && days > 0) {
      return { startDate, endDate: endFromDays(startDate, days), days };
    }
    if (endDate && days && days > 0 && !startDate) {
      return { startDate: startFromDays(endDate, days), endDate, days };
    }
    return draft;
  }

  // editedField === 'startDate' | 'endDate' — даты первичны, дни производные
  if (startDate && endDate) {
    if (endDate.isBefore(startDate, 'day')) {
      // Даты пришли в противоречие — подтягиваем ту дату, которую пользователь
      // только что НЕ трогал, к той, которую он только что изменил.
      if (editedField === 'startDate') {
        return { startDate, endDate: startDate, days: 1 };
      }
      return { startDate: endDate, endDate, days: 1 };
    }
    return { startDate, endDate, days: daysBetweenInclusive(startDate, endDate) };
  }

  if (startDate && days && !endDate) {
    return { startDate, endDate: endFromDays(startDate, days), days };
  }

  if (endDate && days && !startDate) {
    return { startDate: startFromDays(endDate, days), endDate, days };
  }

  return draft;
}

const YEARS_FORMS: [string, string, string] = ['год', 'года', 'лет'];
const MONTHS_FORMS: [string, string, string] = ['месяц', 'месяца', 'месяцев'];

function pluralRu(n: number, forms: [string, string, string]): string {
  const mod10 = n % 10;
  const mod100 = n % 100;
  if (mod10 === 1 && mod100 !== 11) return forms[0];
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) return forms[1];
  return forms[2];
}

/** Считает общий стаж от даты устройства до сегодняшнего дня, например "5 лет 5 месяцев". */
export function formatTenure(hireDate: Dayjs): string {
  const now = dayjs();
  if (hireDate.isAfter(now, 'day')) {
    return '—';
  }

  const years = now.diff(hireDate, 'year');
  const months = now.diff(hireDate.add(years, 'year'), 'month');

  const parts: string[] = [];
  if (years > 0) parts.push(`${years} ${pluralRu(years, YEARS_FORMS)}`);
  parts.push(`${months} ${pluralRu(months, MONTHS_FORMS)}`);

  return parts.join(' ');
}
