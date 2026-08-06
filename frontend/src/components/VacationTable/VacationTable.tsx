import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import { Button, DatePicker, Empty, InputNumber, Popconfirm, Table, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs, { Dayjs } from 'dayjs';
import { useEffect, useState } from 'react';
import { api } from '../../api/client';
import type { Vacation } from '../../api/types';
import { recalcVacation, type VacationDraft, type VacationField } from '../../utils/dateMath';

interface Row {
  key: string;
  id: number | null; // null — черновик, ещё не сохранён на сервере
  startDate: Dayjs | null;
  endDate: Dayjs | null;
  days: number | null;
}

interface Props {
  employeeId: number | null;
  employeeName: string | null;
  vacations: Vacation[];
  /** Может ли текущий пользователь редактировать отпуска ЭТОГО сотрудника (менеджер — всегда; иначе — только свои). */
  canEdit: boolean;
  onChanged: () => void;
}

let rowKeySeq = 0;
const nextDraftKey = () => `draft-${rowKeySeq++}`;

function toRow(v: Vacation): Row {
  return { key: `v-${v.id}`, id: v.id, startDate: dayjs(v.startDate), endDate: dayjs(v.endDate), days: v.days };
}

export default function VacationTable({ employeeId, employeeName, vacations, canEdit, onChanged }: Props) {
  const [rows, setRows] = useState<Row[]>([]);

  useEffect(() => {
    setRows(vacations.map(toRow));
  }, [vacations, employeeId]);

  const applyChange = (row: Row, field: VacationField, value: Dayjs | number | null): Row => {
    const draft: VacationDraft = { startDate: row.startDate, endDate: row.endDate, days: row.days };
    if (field === 'startDate') draft.startDate = value as Dayjs | null;
    else if (field === 'endDate') draft.endDate = value as Dayjs | null;
    else draft.days = value as number | null;

    const recalced = recalcVacation(draft, field);
    const newRow: Row = { ...row, ...recalced };
    setRows((prev) => prev.map((r) => (r.key === row.key ? newRow : r)));
    return newRow;
  };

  const persistRow = async (row: Row) => {
    if (!employeeId || !row.startDate || !row.endDate) return;

    const payload = {
      employeeId,
      startDate: row.startDate.format('YYYY-MM-DD'),
      endDate: row.endDate.format('YYYY-MM-DD'),
    };

    try {
      if (row.id === null) {
        const created = await api.vacations.create(payload);
        setRows((prev) =>
          prev.map((r) => (r.key === row.key ? { ...r, key: `v-${created.id}`, id: created.id, days: created.days } : r)),
        );
      } else {
        await api.vacations.update(row.id, payload);
      }
      onChanged();
    } catch (error) {
      message.error(error instanceof Error ? error.message : 'Не удалось сохранить отпуск');
      // откатываем к последнему сохранённому на сервере состоянию (например,
      // если даты пересеклись с уже существующим отпуском этого сотрудника)
      setRows(vacations.map(toRow));
    }
  };

  const handleAddRow = () => {
    setRows((prev) => [...prev, { key: nextDraftKey(), id: null, startDate: null, endDate: null, days: null }]);
  };

  const handleDeleteRow = async (row: Row) => {
    try {
      if (row.id !== null) {
        await api.vacations.remove(row.id);
        onChanged();
      }
      setRows((prev) => prev.filter((r) => r.key !== row.key));
    } catch (error) {
      message.error(error instanceof Error ? error.message : 'Не удалось удалить отпуск');
    }
  };

  if (!employeeId) {
    return (
      <div className="panel-body">
        <Empty description="Выберите сотрудника слева" image={Empty.PRESENTED_IMAGE_SIMPLE} />
      </div>
    );
  }

  const columns: ColumnsType<Row> = [
    {
      title: 'Начало',
      key: 'startDate',
      width: 126,
      render: (_, row) =>
        canEdit ? (
          <DatePicker
            size="small"
            format="DD.MM.YYYY"
            value={row.startDate}
            onChange={(value) => void persistRow(applyChange(row, 'startDate', value))}
          />
        ) : (
          row.startDate?.format('DD.MM.YYYY') ?? '—'
        ),
    },
    {
      title: 'Окончание',
      key: 'endDate',
      width: 126,
      render: (_, row) =>
        canEdit ? (
          <DatePicker
            size="small"
            format="DD.MM.YYYY"
            value={row.endDate}
            onChange={(value) => void persistRow(applyChange(row, 'endDate', value))}
          />
        ) : (
          row.endDate?.format('DD.MM.YYYY') ?? '—'
        ),
    },
    {
      title: 'Дней',
      key: 'days',
      width: 76,
      render: (_, row) =>
        canEdit ? (
          <InputNumber
            size="small"
            min={1}
            value={row.days ?? undefined}
            onChange={(value) => applyChange(row, 'days', value ?? null)}
            onBlur={() => void persistRow(row)}
          />
        ) : (
          (row.days ?? '—')
        ),
    },
  ];

  if (canEdit) {
    columns.push({
      title: '',
      key: 'actions',
      width: 36,
      render: (_, row) => (
        <Popconfirm title="Удалить запись?" okText="Удалить" cancelText="Отмена" onConfirm={() => handleDeleteRow(row)}>
          <Button size="small" type="text" danger icon={<DeleteOutlined />} />
        </Popconfirm>
      ),
    });
  }

  return (
    <>
      <div className="panel-header">
        <h2>Отпуска — {employeeName}</h2>
        {canEdit && (
          <Button size="small" icon={<PlusOutlined />} onClick={handleAddRow}>
            Добавить
          </Button>
        )}
      </div>
      <div className="panel-body">
        <Table<Row> size="small" rowKey="key" dataSource={rows} columns={columns} pagination={false} />
      </div>
    </>
  );
}
