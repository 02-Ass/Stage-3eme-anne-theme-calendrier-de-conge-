import dayjs, { Dayjs } from 'dayjs';
import { useMemo } from 'react';
import type { Employee, Vacation } from '../../api/types';
import { buildCalendarIndex, surnameOf, type DayBlock } from '../../utils/calendarIndex';

interface Props {
  employees: Employee[];
  vacations: Vacation[];
  year: number;
}

const WEEKDAYS = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'];
const MAX_VISIBLE_CHIPS = 3;

export default function YearCalendar({ employees, vacations, year }: Props) {
  const index = useMemo(() => buildCalendarIndex(employees, vacations), [employees, vacations]);
  const months = useMemo(() => Array.from({ length: 12 }, (_, i) => dayjs(new Date(year, i, 1))), [year]);

  return (
    <div className="year-calendar-grid">
      {months.map((month) => (
        <MonthCard key={month.month()} month={month} index={index} />
      ))}
    </div>
  );
}

function MonthCard({ month, index }: { month: Dayjs; index: Map<string, DayBlock[]> }) {
  const daysInMonth = month.daysInMonth();
  // dayjs().day(): Вс=0 ... Сб=6. Сдвигаем так, чтобы неделя начиналась с понедельника.
  const firstDayOffset = (month.date(1).day() + 6) % 7;

  const cells: (Dayjs | null)[] = [
    ...Array.from({ length: firstDayOffset }, () => null),
    ...Array.from({ length: daysInMonth }, (_, i) => month.date(i + 1)),
  ];
  while (cells.length % 7 !== 0) cells.push(null);

  const weekCount = cells.length / 7;

  return (
    <div className="month-card">
      <div className="month-card-title">{month.format('MMMM YYYY')}</div>
      <div className="month-card-weekdays">
        {WEEKDAYS.map((wd) => (
          <div key={wd}>{wd}</div>
        ))}
      </div>
      <div>
        {Array.from({ length: weekCount }, (_, weekIndex) => (
          <div className="month-card-week" key={weekIndex}>
            {cells.slice(weekIndex * 7, weekIndex * 7 + 7).map((day, dayIdx) => (
              <DayCell key={dayIdx} day={day} index={index} />
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

function DayCell({ day, index }: { day: Dayjs | null; index: Map<string, DayBlock[]> }) {
  if (!day) {
    return <div className="month-day-cell" />;
  }

  const blocks = index.get(day.format('YYYY-MM-DD')) ?? [];
  const visible = blocks.slice(0, MAX_VISIBLE_CHIPS);
  const hidden = blocks.length - visible.length;

  return (
    <div className="month-day-cell">
      <div className="month-day-number">{day.date()}</div>
      {visible.map((block) => (
        <div
          key={block.employeeId}
          className={`vacation-chip${block.conflict ? ' conflict' : ''}`}
          style={{ background: block.color }}
          title={block.conflict ? `${block.fullName} — пересечение отпусков на проекте` : block.fullName}
        >
          {surnameOf(block.fullName)}
        </div>
      ))}
      {hidden > 0 && <div className="vacation-chip-more">+{hidden}</div>}
    </div>
  );
}
