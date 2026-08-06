import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { Button, Empty, Modal, Space, Tooltip } from 'antd';
import type { Employee } from '../../api/types';

interface Props {
  employees: Employee[];
  selectedEmployeeId: number | null;
  isManager: boolean;
  onSelect: (id: number) => void;
  onAdd: () => void;
  onEdit: (employee: Employee) => void;
  onDelete: (employee: Employee) => Promise<void>;
}

export default function EmployeeSidebar({
  employees,
  selectedEmployeeId,
  isManager,
  onSelect,
  onAdd,
  onEdit,
  onDelete,
}: Props) {
  const selected = employees.find((e) => e.id === selectedEmployeeId) ?? null;

  const handleDeleteClick = () => {
    if (!selected) return;
    Modal.confirm({
      title: 'Удалить сотрудника?',
      content: `«${selected.fullName}» будет удалён вместе со всеми его отпусками. Действие необратимо.`,
      okText: 'Удалить',
      okButtonProps: { danger: true },
      cancelText: 'Отмена',
      onOk: () => onDelete(selected),
    });
  };

  return (
    <>
      <div className="panel-header">
        <h2>Список сотрудников</h2>
        {isManager && (
          <Space size={4}>
            <Tooltip title="Добавить сотрудника">
              <Button size="small" icon={<PlusOutlined />} onClick={onAdd} />
            </Tooltip>
            <Tooltip title="Редактировать">
              <Button
                size="small"
                icon={<EditOutlined />}
                disabled={!selected}
                onClick={() => selected && onEdit(selected)}
              />
            </Tooltip>
            <Tooltip title="Удалить">
              <Button size="small" danger icon={<DeleteOutlined />} disabled={!selected} onClick={handleDeleteClick} />
            </Tooltip>
          </Space>
        )}
      </div>
      <div className="panel-body">
        {employees.length === 0 && <Empty description="Нет сотрудников" image={Empty.PRESENTED_IMAGE_SIMPLE} />}
        {employees.map((employee) => (
          <div
            key={employee.id}
            className={`employee-row${employee.id === selectedEmployeeId ? ' selected' : ''}`}
            onClick={() => onSelect(employee.id)}
          >
            <span className="employee-color-dot" style={{ background: employee.color }} />
            <span>{employee.fullName}</span>
          </div>
        ))}
      </div>
    </>
  );
}
