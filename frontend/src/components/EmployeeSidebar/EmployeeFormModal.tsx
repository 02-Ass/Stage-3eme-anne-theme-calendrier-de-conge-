import { useEffect, useMemo, useState } from 'react';
import { ColorPicker, DatePicker, Form, Input, Modal, Select } from 'antd';
import dayjs, { Dayjs } from 'dayjs';
import type { Employee } from '../../api/types';
import { formatTenure } from '../../utils/dateMath';

interface EmployeeFormValues {
  fullName: string;
  hireDate: Dayjs;
  projects: string[];
  // ColorPicker отдаёт свой внутренний класс Color при изменении и
  // обычную hex-строку при первичной установке значения через setFieldsValue.
  color: string | { toHexString: () => string };
  email: string | null;
  keycloakUserId: string | null;
}

export interface EmployeeFormSubmitValues {
  fullName: string;
  hireDate: string;
  projects: string[];
  color: string;
  email: string | null;
  keycloakUserId: string | null;
}

interface Props {
  open: boolean;
  employee: Employee | null; // null = создание нового сотрудника
  existingProjectNames: string[];
  onCancel: () => void;
  onSubmit: (values: EmployeeFormSubmitValues) => Promise<void>;
}

const DEFAULT_COLOR = '#95de64';

export default function EmployeeFormModal({ open, employee, existingProjectNames, onCancel, onSubmit }: Props) {
  const [form] = Form.useForm<EmployeeFormValues>();
  const [saving, setSaving] = useState(false);
  const hireDate = Form.useWatch('hireDate', form);

  useEffect(() => {
    if (!open) return;

    if (employee) {
      form.setFieldsValue({
        fullName: employee.fullName,
        hireDate: dayjs(employee.hireDate),
        projects: employee.projects,
        color: employee.color,
        email: employee.email,
        keycloakUserId: employee.keycloakUserId,
      });
    } else {
      form.resetFields();
      form.setFieldsValue({ color: DEFAULT_COLOR, projects: [] });
    }
  }, [open, employee, form]);

  const tenureHint = useMemo(() => {
    if (!hireDate) return null;
    return `общий стаж: ${formatTenure(hireDate)}`;
  }, [hireDate]);

  const projectOptions = useMemo(
    () => existingProjectNames.map((name) => ({ value: name, label: name })),
    [existingProjectNames],
  );

  const handleOk = async () => {
    const values = await form.validateFields();
    setSaving(true);
    try {
      const colorHex = typeof values.color === 'string' ? values.color : values.color.toHexString();
      await onSubmit({
        fullName: values.fullName.trim(),
        hireDate: values.hireDate.format('YYYY-MM-DD'),
        projects: values.projects ?? [],
        color: colorHex,
        email: values.email?.trim() || null,
        keycloakUserId: values.keycloakUserId?.trim() || null,
      });
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title={employee ? 'Редактирование сотрудника' : 'Новый сотрудник'}
      open={open}
      onCancel={onCancel}
      onOk={handleOk}
      confirmLoading={saving}
      okText="Сохранить"
      cancelText="Отмена"
      destroyOnHidden
    >
      <Form<EmployeeFormValues> form={form} layout="vertical" requiredMark={false}>
        <Form.Item
          name="fullName"
          label="Имя сотрудника"
          rules={[{ required: true, message: 'Введите имя сотрудника' }]}
        >
          <Input placeholder="Иванов И.И." />
        </Form.Item>

        <Form.Item
          name="hireDate"
          label="Дата устройства"
          rules={[{ required: true, message: 'Укажите дату устройства на работу' }]}
          extra={tenureHint}
        >
          <DatePicker format="DD.MM.YYYY" style={{ width: '100%' }} />
        </Form.Item>

        <Form.Item name="projects" label="Проекты">
          <Select mode="tags" placeholder="Добавьте проект и нажмите Enter" options={projectOptions} tokenSeparators={[',']} />
        </Form.Item>

        <Form.Item name="color" label="Цвет в календаре" rules={[{ required: true, message: 'Выберите цвет' }]}>
          <ColorPicker format="hex" />
        </Form.Item>

        <Form.Item
          name="email"
          label="Email"
          rules={[{ type: 'email', message: 'Введите корректный email' }]}
          extra="Для напоминаний о приближающемся отпуске"
        >
          <Input placeholder="ivanov@example.com" />
        </Form.Item>

        <Form.Item
          name="keycloakUserId"
          label="Keycloak user ID"
          extra="Необязательно. Связывает сотрудника с логином — тогда он увидит и сможет редактировать только свои данные. ID пользователя — в Keycloak: Users → нужный пользователь → поле «ID» вверху."
        >
          <Input placeholder="например, a1b2c3d4-e5f6-..." />
        </Form.Item>
      </Form>
    </Modal>
  );
}
