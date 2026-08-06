import { describe, expect, it } from 'vitest';
import type { Employee, Vacation } from '../api/types';
import { buildCalendarIndex, surnameOf } from './calendarIndex';

function makeEmployee(overrides: Partial<Employee> & { id: number; fullName: string }): Employee {
  return {
    hireDate: '2020-01-01',
    color: '#1677ff',
    projects: [],
    email: null,
    keycloakUserId: null,
    ...overrides,
  };
}

function makeVacation(overrides: Partial<Vacation> & { id: number; employeeId: number }): Vacation {
  return {
    startDate: '2026-07-01',
    endDate: '2026-07-01',
    days: 1,
    ...overrides,
  };
}

describe('buildCalendarIndex', () => {
  it('два сотрудника без общих проектов, отпуска пересекаются — конфликта нет', () => {
    const employees = [
      makeEmployee({ id: 1, fullName: 'Иванов И.И.', projects: ['Проект A'] }),
      makeEmployee({ id: 2, fullName: 'Петров П.П.', projects: ['Проект B'] }),
    ];
    const vacations = [
      makeVacation({ id: 1, employeeId: 1, startDate: '2026-07-01', endDate: '2026-07-05', days: 5 }),
      makeVacation({ id: 2, employeeId: 2, startDate: '2026-07-03', endDate: '2026-07-07', days: 5 }),
    ];

    const index = buildCalendarIndex(employees, vacations);
    const day = index.get('2026-07-03')!;

    expect(day).toHaveLength(2);
    expect(day.every((b) => b.conflict === false)).toBe(true);
  });

  it('два сотрудника с общим проектом, отпуска пересекаются — оба помечены конфликтом', () => {
    const employees = [
      makeEmployee({ id: 1, fullName: 'Иванов И.И.', projects: ['Проект A'] }),
      makeEmployee({ id: 2, fullName: 'Петров П.П.', projects: ['Проект A', 'Проект B'] }),
    ];
    const vacations = [
      makeVacation({ id: 1, employeeId: 1, startDate: '2026-07-01', endDate: '2026-07-05', days: 5 }),
      makeVacation({ id: 2, employeeId: 2, startDate: '2026-07-03', endDate: '2026-07-07', days: 5 }),
    ];

    const index = buildCalendarIndex(employees, vacations);

    // 3–5 июля — общие дни пересечения, оба должны быть помечены конфликтом
    for (const key of ['2026-07-03', '2026-07-04', '2026-07-05']) {
      const day = index.get(key)!;
      expect(day.every((b) => b.conflict === true)).toBe(true);
    }

    // 1–2 июля — только Иванов, конфликта нет (Петров ещё не в отпуске)
    const day1 = index.get('2026-07-01')!;
    expect(day1).toHaveLength(1);
    expect(day1[0].conflict).toBe(false);
  });

  it('общий проект, но отпуска НЕ пересекаются по датам — конфликта нет', () => {
    const employees = [
      makeEmployee({ id: 1, fullName: 'Иванов И.И.', projects: ['Проект A'] }),
      makeEmployee({ id: 2, fullName: 'Петров П.П.', projects: ['Проект A'] }),
    ];
    const vacations = [
      makeVacation({ id: 1, employeeId: 1, startDate: '2026-07-01', endDate: '2026-07-05', days: 5 }),
      makeVacation({ id: 2, employeeId: 2, startDate: '2026-08-01', endDate: '2026-08-05', days: 5 }),
    ];

    const index = buildCalendarIndex(employees, vacations);

    for (const [, blocks] of index) {
      expect(blocks.every((b) => b.conflict === false)).toBe(true);
    }
  });

  it('один сотрудник в отпуске — конфликта быть не может', () => {
    const employees = [makeEmployee({ id: 1, fullName: 'Иванов И.И.', projects: ['Проект A'] })];
    const vacations = [makeVacation({ id: 1, employeeId: 1, startDate: '2026-07-01', endDate: '2026-07-03', days: 3 })];

    const index = buildCalendarIndex(employees, vacations);
    const day = index.get('2026-07-02')!;

    expect(day).toHaveLength(1);
    expect(day[0].conflict).toBe(false);
  });

  it('блоки внутри дня отсортированы по имени', () => {
    const employees = [
      makeEmployee({ id: 1, fullName: 'Яковлев Я.Я.', projects: [] }),
      makeEmployee({ id: 2, fullName: 'Алексеев А.А.', projects: [] }),
    ];
    const vacations = [
      makeVacation({ id: 1, employeeId: 1, startDate: '2026-07-01', endDate: '2026-07-01', days: 1 }),
      makeVacation({ id: 2, employeeId: 2, startDate: '2026-07-01', endDate: '2026-07-01', days: 1 }),
    ];

    const index = buildCalendarIndex(employees, vacations);
    const day = index.get('2026-07-01')!;

    expect(day.map((b) => b.fullName)).toEqual(['Алексеев А.А.', 'Яковлев Я.Я.']);
  });
});

describe('surnameOf', () => {
  it('берёт первое слово из ФИО', () => {
    expect(surnameOf('Иванов И.И.')).toBe('Иванов');
  });

  it('одно слово — возвращает как есть', () => {
    expect(surnameOf('Иванов')).toBe('Иванов');
  });

  it('обрезает пробелы по краям', () => {
    expect(surnameOf('  Иванов И.И.  ')).toBe('Иванов');
  });
});
