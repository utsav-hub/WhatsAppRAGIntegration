import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { api } from '../api';

type Conversation = {
  id: number;
  phoneNumber: string;
  userMessage: string;
  botResponse?: string | null;
  timestamp: string;
};

export function ConversationsPage() {
  const [phoneFilter, setPhoneFilter] = useState('');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['conversations', phoneFilter],
    queryFn: async () => {
      const res = await api.get<Conversation[]>('/api/admin/conversations', {
        params: {
          phoneNumber: phoneFilter || undefined,
          take: 100
        }
      });
      return res.data;
    }
  });

  return (
    <div>
      <h1>Conversations</h1>
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

      {isLoading && <div>Loading conversations...</div>}
      {isError && <div>Failed to load conversations.</div>}

      {data && (
        <div className="table-wrapper">
          <table className="table">
            <thead>
              <tr>
                <th>Time</th>
                <th>Phone</th>
                <th>User message</th>
                <th>Bot response</th>
              </tr>
            </thead>
            <tbody>
              {data.map((c) => (
                <tr key={c.id}>
                  <td>{dayjs(c.timestamp).format('YYYY-MM-DD HH:mm')}</td>
                  <td>{c.phoneNumber}</td>
                  <td>{c.userMessage}</td>
                  <td>{c.botResponse}</td>
                </tr>
              ))}
              {data.length === 0 && (
                <tr>
                  <td colSpan={4} className="muted">
                    No conversations found.
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

