import dayjs from 'dayjs';
import type { Employee, Vacation } from '../api/types';

export interface DayBlock {
  employeeId: number;
  fullName: string;
  color: string;
  /** true, если в этот день пересекается отпуск с другим сотрудником того же проекта */
  conflict: boolean;
}

/**
 * Строит индекс "дата -> список сотрудников в отпуске в этот день" и
 * помечает конфликтующие записи: конфликт — это день, когда в отпуске
 * одновременно находятся два РАЗНЫХ сотрудника, у которых есть хотя бы
 * один общий проект (см. ТЗ, раздел "Календарь").
 */
export function buildCalendarIndex(
  employees: Employee[],
  vacations: Vacation[],
): Map<string, DayBlock[]> {
  const employeeById = new Map(employees.map((e) => [e.id, e]));

  // 1. Группируем активные отпуска по каждому дню
  const dayVacations = new Map<string, Vacation[]>();
  for (const vacation of vacations) {
    const end = dayjs(vacation.endDate);
    let cursor = dayjs(vacation.startDate);
    while (cursor.isSame(end, 'day') || cursor.isBefore(end, 'day')) {
      const key = cursor.format('YYYY-MM-DD');
      const list = dayVacations.get(key);
      if (list) {
        list.push(vacation);
      } else {
        dayVacations.set(key, [vacation]);
      }
      cursor = cursor.add(1, 'day');
    }
  }

  // 2. Для каждого дня ищем пары сотрудников с общим проектом
  const index = new Map<string, DayBlock[]>();

  for (const [key, dayVacs] of dayVacations) {
    const conflictEmployeeIds = new Set<number>();

    for (let i = 0; i < dayVacs.length; i += 1) {
      for (let j = i + 1; j < dayVacs.length; j += 1) {
        const e1 = employeeById.get(dayVacs[i].employeeId);
        const e2 = employeeById.get(dayVacs[j].employeeId);
        if (!e1 || !e2 || e1.id === e2.id) continue;

        const sharesProject = e1.projects.some((p) => e2.projects.includes(p));
        if (sharesProject) {
          conflictEmployeeIds.add(e1.id);
          conflictEmployeeIds.add(e2.id);
        }
      }
    }

    const blocks = dayVacs
      .map((v): DayBlock | null => {
        const employee = employeeById.get(v.employeeId);
        if (!employee) return null;
        return {
          employeeId: employee.id,
          fullName: employee.fullName,
          color: employee.color,
          conflict: conflictEmployeeIds.has(employee.id),
        };
      })
      .filter((b): b is DayBlock => b !== null)
      .sort((a, b) => a.fullName.localeCompare(b.fullName, 'ru'));

    index.set(key, blocks);
  }

  return index;
}

/** Короткая фамилия для отображения в клетке календаря (первое слово из ФИО). */
export function surnameOf(fullName: string): string {
  return fullName.trim().split(/\s+/)[0] ?? fullName;
}
