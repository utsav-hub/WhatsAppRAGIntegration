import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { api } from '../api';

type Lead = {
  id: number;
  phoneNumber: string;
  requirement: string;
  createdDate: string;
};

export function LeadsPage() {
  const [phoneFilter, setPhoneFilter] = useState('');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['leads', phoneFilter],
    queryFn: async () => {
      const res = await api.get<Lead[]>('/api/admin/leads', {
        params: {
          phoneNumber: phoneFilter || undefined
        }
      });
      return res.data;
    }
  });

  return (
    <div>
      <h1>Leads</h1>
      <div className="filters">
        <input
          className="input"
          placeholder="Filter by phone (optional)"
          value={phoneFilter}
          onChange={(e) => setPhoneFilter(e.target.value)}
        />
        <button className="button" onClick={() => refetch()}>
          Apply
        </button>
      </div>

      {isLoading && <div>Loading leads...</div>}
      {isError && <div>Failed to load leads.</div>}

      {data && (
        <div className="table-wrapper">
          <table className="table">
            <thead>
              <tr>
                <th>Created</th>
                <th>Phone</th>
                <th>Requirement</th>
              </tr>
            </thead>
            <tbody>
              {data.map((l) => (
                <tr key={l.id}>
                  <td>{dayjs(l.createdDate).format('YYYY-MM-DD HH:mm')}</td>
                  <td>{l.phoneNumber}</td>
                  <td>{l.requirement}</td>
                </tr>
              ))}
              {data.length === 0 && (
                <tr>
                  <td colSpan={3} className="muted">
                    No leads found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

