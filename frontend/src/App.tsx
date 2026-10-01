import { DownloadOutlined } from '@ant-design/icons';
import { Button, Spin, Typography, message } from 'antd';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { api, registerTokenGetter } from './api/client';
import type { Employee, Me, Project, Vacation } from './api/types';
import { useAppAuth } from './auth/useAppAuth';
import EmployeeFormModal, { type EmployeeFormSubmitValues } from './components/EmployeeSidebar/EmployeeFormModal';
import EmployeeSidebar from './components/EmployeeSidebar/EmployeeSidebar';
import UpcomingNotifications from './components/Notifications/UpcomingNotifications';
import VacationTable from './components/VacationTable/VacationTable';
import YearCalendar from './components/YearCalendar/YearCalendar';

const CURRENT_YEAR = new Date().getFullYear();

export default function App() {
  const auth = useAppAuth();

  const [employees, setEmployees] = useState<Employee[]>([]);
  const [vacations, setVacations] = useState<Vacation[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [me, setMe] = useState<Me | null>(null);
  const [initialLoading, setInitialLoading] = useState(true);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null);

  // Прокидываем access_token из react-oidc-context в API-клиент
  useEffect(() => {
    registerTokenGetter(() => auth.user?.access_token);
  }, [auth.user]);

  const loadAll = useCallback(async () => {
    try {
      const [employeesData, vacationsData, projectsData, meData] = await Promise.all([
        api.employees.list(),
        api.vacations.list(CURRENT_YEAR),
        api.projects.list(),
        api.me.get(),
      ]);
      setEmployees(employeesData);
      setVacations(vacationsData);
      setProjects(projectsData);
      setMe(meData);
    } catch (error) {
      void message.error(error instanceof Error ? error.message : 'Не удалось загрузить данные');
    } finally {
      setInitialLoading(false);
    }
  }, []);

  const loadVacations = useCallback(async () => {
    try {
      const vacationsData = await api.vacations.list(CURRENT_YEAR);
      setVacations(vacationsData);
    } catch (error) {
      void message.error(error instanceof Error ? error.message : 'Не удалось обновить отпуска');
    }
  }, []);

  useEffect(() => {
    if (auth.isAuthenticated) {
      void loadAll();
    }
  }, [auth.isAuthenticated, loadAll]);

  const selectedEmployee = useMemo(
    () => employees.find((e) => e.id === selectedEmployeeId) ?? null,
    [employees, selectedEmployeeId],
  );

  const employeeVacations = useMemo(
    () => vacations.filter((v) => v.employeeId === selectedEmployeeId),
    [vacations, selectedEmployeeId],
  );

  const existingProjectNames = useMemo(() => projects.map((p) => p.name), [projects]);

  const isManager = me?.isManager ?? false;
  const canEditSelected = isManager || (me?.employeeId !== null && me?.employeeId === selectedEmployeeId);

  const handleAddEmployee = () => {
    setEditingEmployee(null);
    setModalOpen(true);
  };

  const handleEditEmployee = (employee: Employee) => {
    setEditingEmployee(employee);
    setModalOpen(true);
  };

  const handleDeleteEmployee = async (employee: Employee) => {
    try {
      await api.employees.remove(employee.id);
      if (selectedEmployeeId === employee.id) setSelectedEmployeeId(null);
      await loadAll();
    } catch (error) {
      void message.error(error instanceof Error ? error.message : 'Не удалось удалить сотрудника');
    }
  };

  const handleSubmitEmployee = async (values: EmployeeFormSubmitValues) => {
    try {
      if (editingEmployee) {
        await api.employees.update(editingEmployee.id, values);
      } else {
        const created = await api.employees.create(values);
        setSelectedEmployeeId(created.id);
      }
      setModalOpen(false);
      await loadAll();
    } catch (error) {
      void message.error(error instanceof Error ? error.message : 'Не удалось сохранить сотрудника');
    }
  };

  const handleExport = async () => {
    try {
      const blob = await api.vacations.exportCsv(CURRENT_YEAR);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `vacations_${CURRENT_YEAR}.csv`;
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      void message.error(error instanceof Error ? error.message : 'Не удалось экспортировать данные');
    }
  };

  if (auth.isLoading) {
    return (
      <div className="login-screen">
        <Spin size="large" />
      </div>
    );
  }

  if (auth.error) {
    return (
      <div className="login-screen">
        <Typography.Title level={4}>Ошибка авторизации</Typography.Title>
        <Typography.Text type="secondary">{auth.error.message}</Typography.Text>
        <Button type="primary" onClick={() => void auth.signinRedirect()}>
          Попробовать снова
        </Button>
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    return (
      <div className="login-screen">
        <Typography.Title level={3}>Vacation planning</Typography.Title>
        <Button type="primary" onClick={() => void auth.signinRedirect()}>
          Login by Keycloak
        </Button>
      </div>
    );
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <h1>Vacation planning</h1>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <UpcomingNotifications employees={employees} />
          <Button
            size="small"
            onClick={() => void auth.signoutRedirect({ post_logout_redirect_uri: window.location.origin })}
          >
            Login ({auth.user?.profile.preferred_username ?? auth.user?.profile.email ?? 'пользователь'})
          </Button>
        </div>
      </header>

      {initialLoading ? (
        <div className="app-body" style={{ alignItems: 'center', justifyContent: 'center' }}>
          <Spin size="large" />
        </div>
      ) : (
        <div className="app-body">
          <section className="panel sidebar-panel">
            <EmployeeSidebar
              employees={employees}
              selectedEmployeeId={selectedEmployeeId}
              isManager={isManager}
              onSelect={setSelectedEmployeeId}
              onAdd={handleAddEmployee}
              onEdit={handleEditEmployee}
              onDelete={handleDeleteEmployee}
            />
          </section>

          <section className="panel vacations-panel">
            <VacationTable
              employeeId={selectedEmployeeId}
              employeeName={selectedEmployee?.fullName ?? null}
              vacations={employeeVacations}
              canEdit={canEditSelected}
              onChanged={loadVacations}
            />
          </section>

          <section className="panel calendar-panel">
            <div className="panel-header">
              <h2>Calendar — {CURRENT_YEAR}</h2>
              {isManager && (
                <Button size="small" icon={<DownloadOutlined />} onClick={() => void handleExport()}>
                  Export in CSV
                </Button>
              )}
            </div>
            <div className="panel-body">
              <YearCalendar employees={employees} vacations={vacations} year={CURRENT_YEAR} />
            </div>
          </section>
        </div>
      )}

      <EmployeeFormModal
        open={modalOpen}
        employee={editingEmployee}
        existingProjectNames={existingProjectNames}
        onCancel={() => setModalOpen(false)}
        onSubmit={handleSubmitEmployee}
      />
    </div>
  );
}
