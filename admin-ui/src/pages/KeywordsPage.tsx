import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { api } from '../api';

type ChatbotKeyword = {
  id: string;
  keyword: string;
  isActive: boolean;
  createdAt: string;
};

export function KeywordsPage() {
  const [newKeyword, setNewKeyword] = useState('');
  const [newActive, setNewActive] = useState(true);
  const queryClient = useQueryClient();

  const { data: keywords, isLoading, isError } = useQuery({
    queryKey: ['chatbot-keywords'],
    queryFn: async () => {
      const res = await api.get<ChatbotKeyword[]>('/api/chatbot/keywords');
      return res.data;
    }
  });

  const createMutation = useMutation({
    mutationFn: async (payload: { keyword: string; isActive: boolean }) => {
      await api.post('/api/chatbot/keywords', payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chatbot-keywords'] });
      setNewKeyword('');
      setNewActive(true);
    }
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/chatbot/keywords/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chatbot-keywords'] });
    }
  });

  const handleAdd = () => {
    const trimmed = newKeyword.trim();
    if (!trimmed) return;
    createMutation.mutate({ keyword: trimmed, isActive: newActive });
  };

  return (
    <div>
      <h1>Chatbot keywords</h1>
      <p className="muted">
        Domain keywords for in-scope detection. If any keyword exists and the user message contains none, the bot returns the out-of-scope reply.
      </p>

      <div className="card" style={{ marginTop: '1rem', marginBottom: '1.5rem' }}>
        <div className="card-title">Add keyword</div>
        <div className="filters" style={{ marginBottom: 0 }}>
          <input
            className="input"
            placeholder="e.g. shipment, track, freight"
            value={newKeyword}
            onChange={(e) => setNewKeyword(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleAdd()}
          />
          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.9rem' }}>
            <input
              type="checkbox"
              checked={newActive}
              onChange={(e) => setNewActive(e.target.checked)}
            />
            Active
          </label>
          <button
            className="button"
            onClick={handleAdd}
            disabled={!newKeyword.trim() || createMutation.isPending}
          >
            {createMutation.isPending ? 'Adding…' : 'Add'}
          </button>
        </div>
        {createMutation.isError && (
          <div style={{ color: 'var(--text-muted)', fontSize: '0.85rem', marginTop: '0.5rem' }}>
            Failed to add keyword.
          </div>
        )}
      </div>

      {isLoading && <div>Loading keywords...</div>}
      {isError && <div>Failed to load keywords.</div>}

      {keywords && (
        <div className="table-wrapper">
          <table className="table">
            <thead>
              <tr>
                <th>Keyword</th>
                <th>Status</th>
                <th>Added</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {keywords.map((k) => (
                <tr key={k.id}>
                  <td>{k.keyword}</td>
                  <td>{k.isActive ? 'Active' : 'Inactive'}</td>
                  <td>{dayjs(k.createdAt).format('YYYY-MM-DD HH:mm')}</td>
                  <td>
                    <button
                      type="button"
                      className="button"
                      style={{ padding: '0.35rem 0.6rem', fontSize: '0.8rem', background: 'rgba(239, 68, 68, 0.9)' }}
                      onClick={() => deleteMutation.mutate(k.id)}
                      disabled={deleteMutation.isPending}
                    >
                      Remove
                    </button>
                  </td>
                </tr>
              ))}
              {keywords.length === 0 && (
                <tr>
                  <td colSpan={4} className="muted">
                    No keywords yet. Add one above to restrict replies to domain-related messages.
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
