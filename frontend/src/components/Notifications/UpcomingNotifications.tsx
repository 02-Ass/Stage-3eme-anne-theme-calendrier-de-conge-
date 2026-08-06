import { BellOutlined } from '@ant-design/icons';
import { Badge, Dropdown, Empty, List } from 'antd';
import dayjs from 'dayjs';
import { useEffect, useState } from 'react';
import { api } from '../../api/client';
import type { Employee, Vacation } from '../../api/types';

interface Props {
  employees: Employee[];
  daysAhead?: number;
}

/** Колокольчик с отпусками, которые начинаются в ближайшие daysAhead дней. */
export default function UpcomingNotifications({ employees, daysAhead = 14 }: Props) {
  const [upcoming, setUpcoming] = useState<Vacation[]>([]);

  useEffect(() => {
    let cancelled = false;

    api.vacations
      .upcoming(daysAhead)
      .then((data) => {
        if (!cancelled) setUpcoming(data);
      })
      .catch(() => {
        // уведомления не критичны для основного функционала — тихо игнорируем
      });

    return () => {
      cancelled = true;
    };
  }, [daysAhead]);

  const employeeName = (id: number) => employees.find((e) => e.id === id)?.fullName ?? 'Сотрудник';

  const dropdownContent = (
    <div
      style={{
        width: 320,
        maxHeight: 360,
        overflow: 'auto',
        background: '#fff',
        borderRadius: 8,
        boxShadow: '0 2px 8px rgba(0,0,0,0.15)',
      }}
    >
      {upcoming.length === 0 ? (
        <div style={{ padding: 16 }}>
          <Empty description={`Нет отпусков в ближайшие ${daysAhead} дн.`} image={Empty.PRESENTED_IMAGE_SIMPLE} />
        </div>
      ) : (
        <List
          size="small"
          dataSource={upcoming}
          renderItem={(vacation) => (
            <List.Item style={{ padding: '8px 16px' }}>
              <div>
                <div style={{ fontWeight: 600 }}>{employeeName(vacation.employeeId)}</div>
                <div style={{ fontSize: 12, color: '#8c8c8c' }}>
                  начинается {dayjs(vacation.startDate).format('DD.MM.YYYY')} ({vacation.days} дн.)
                </div>
              </div>
            </List.Item>
          )}
        />
      )}
    </div>
  );

  return (
    <Dropdown dropdownRender={() => dropdownContent} trigger={['click']} placement="bottomRight">
      <Badge count={upcoming.length} size="small" offset={[-2, 2]}>
        <BellOutlined style={{ fontSize: 18, cursor: 'pointer', color: '#595959' }} />
      </Badge>
    </Dropdown>
  );
}
