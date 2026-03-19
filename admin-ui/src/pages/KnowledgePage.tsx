import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { api } from '../api';

type KnowledgeDocument = {
  id: string;
  title: string;
  content: string;
  createdAt: string;
};

type KnowledgeIngestionJobDto = {
  id: string;
  title: string;
  status: number;
  createdAt: string;
  startedAt?: string | null;
  finishedAt?: string | null;
  errorMessage?: string | null;
};

function statusLabel(status: number) {
  switch (status) {
    case 0:
      return 'Queued';
    case 1:
      return 'Running';
    case 2:
      return 'Succeeded';
    case 3:
      return 'Failed';
    default:
      return `Unknown(${status})`;
  }
}

const JOB_KEY = 'knowledgeActiveJobId';

export function KnowledgePage() {
  const queryClient = useQueryClient();

  const [title, setTitle] = useState('');
  const [file, setFile] = useState<File | null>(null);

  const initialJobId = useMemo(() => {
    if (typeof window === 'undefined') return null;
    return window.localStorage.getItem(JOB_KEY);
  }, []);

  const [activeJobId, setActiveJobId] = useState<string | null>(initialJobId);
  const [activeJob, setActiveJob] = useState<KnowledgeIngestionJobDto | null>(null);

  const knowledgeQuery = useQuery({
    queryKey: ['knowledge-docs'],
    queryFn: async () => {
      const res = await api.get<KnowledgeDocument[]>('/api/knowledge');
      return res.data;
    }
  });

  const jobQuery = useQuery({
    queryKey: ['knowledge-job', activeJobId],
    queryFn: async () => {
      if (!activeJobId) throw new Error('No active job id');
      const res = await api.get<KnowledgeIngestionJobDto>(`/api/knowledge/ingestion-jobs/${activeJobId}`);
      return res.data;
    },
    enabled: !!activeJobId
  });

  // Poll job status and persist jobId across refreshes.
  useEffect(() => {
    if (!activeJobId) return;

    let stopped = false;
    let intervalId: number | undefined;

    const poll = async () => {
      try {
        const res = await api.get<KnowledgeIngestionJobDto>(`/api/knowledge/ingestion-jobs/${activeJobId}`);
        if (stopped) return;
        setActiveJob(res.data);

        if (res.data.status === 2) {
          // Succeeded: clear persisted job id and refresh docs.
          window.localStorage.removeItem(JOB_KEY);
          setActiveJobId(null);
          setActiveJob(null);
          await queryClient.invalidateQueries({ queryKey: ['knowledge-docs'] });
        }

        if (res.data.status === 3) {
          // Failed: stop polling but keep jobId until cleared by user.
          if (intervalId) window.clearInterval(intervalId);
        }
      } catch (e) {
        // keep polling; UI will continue showing last known status
      }
    };

    // immediate poll
    void poll();

    intervalId = window.setInterval(() => {
      void poll();
    }, 2000);

    return () => {
      stopped = true;
      if (intervalId) window.clearInterval(intervalId);
    };
  }, [activeJobId, queryClient]);

  const uploadMutation = useMutation({
    mutationFn: async () => {
      if (!title.trim()) throw new Error('Title is required.');
      if (!file) throw new Error('File is required.');

      const formData = new FormData();
      formData.append('title', title.trim());
      formData.append('file', file);

      const res = await api.post<{ jobId: string }>('/api/knowledge/upload', formData, {
        headers: {
          // Let axios set boundaries; this header is optional but keeps proxies happy.
          'Content-Type': 'multipart/form-data'
        }
      });
      return res.data;
    },
    onSuccess: async (data) => {
      window.localStorage.setItem(JOB_KEY, data.jobId);
      setActiveJobId(data.jobId);
      setActiveJob(null);
      await queryClient.invalidateQueries({ queryKey: ['knowledge-docs'] });
    }
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/knowledge/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['knowledge-docs'] });
    }
  });

  const canUpload = !activeJobId || activeJob?.status === 2 || activeJob?.status === 3;

  return (
    <div>
      <h1>Knowledge Base</h1>

      <div className="card" style={{ marginTop: '1rem', marginBottom: '1.5rem' }}>
        <div className="card-title">Upload PDF/DOCX (async)</div>
        <div className="filters" style={{ marginBottom: 0 }}>
          <input
            className="input"
            placeholder="Title (e.g. Octology Shipping SOP)"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            disabled={!canUpload}
          />
          <input
            className="input"
            type="file"
            accept=".pdf,.docx"
            onChange={(e) => setFile(e.target.files && e.target.files[0] ? e.target.files[0] : null)}
            disabled={!canUpload}
          />
          <button
            className="button"
            onClick={() => uploadMutation.mutate()}
            disabled={!canUpload || uploadMutation.isPending}
          >
            {uploadMutation.isPending ? 'Uploading…' : 'Upload'}
          </button>
        </div>

        {activeJobId && (
          <div style={{ marginTop: '0.9rem', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
            <div>
              Job: <b>{activeJob ? statusLabel(activeJob.status) : 'Loading…'}</b>
            </div>
            {activeJob?.errorMessage ? (
              <div style={{ marginTop: '0.5rem', color: '#f87171' }}>
                Error: {activeJob.errorMessage}
              </div>
            ) : null}
          </div>
        )}

        {activeJob?.status === 3 ? (
          <button
            type="button"
            className="button"
            style={{
              marginTop: '0.75rem',
              background: 'linear-gradient(135deg, #ef4444, #dc2626)',
              boxShadow: '0 10px 30px rgba(220, 38, 38, 0.35)'
            }}
            onClick={() => {
              window.localStorage.removeItem(JOB_KEY);
              setActiveJobId(null);
              setActiveJob(null);
            }}
          >
            Clear failed job
          </button>
        ) : null}

        {uploadMutation.isError ? (
          <div style={{ marginTop: '0.75rem', color: '#f87171', fontSize: '0.9rem' }}>
            Upload failed. Check backend logs.
          </div>
        ) : null}
      </div>

      <div className="table-wrapper">
        <table className="table">
          <thead>
            <tr>
              <th>Title</th>
              <th>Created</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {knowledgeQuery.isLoading && (
              <tr>
                <td colSpan={3}>Loading knowledge…</td>
              </tr>
            )}
            {knowledgeQuery.isError && (
              <tr>
                <td colSpan={3}>Failed to load knowledge documents.</td>
              </tr>
            )}
            {knowledgeQuery.data &&
              knowledgeQuery.data.map((d) => (
                <tr key={d.id}>
                  <td>{d.title}</td>
                  <td>{dayjs(d.createdAt).format('YYYY-MM-DD HH:mm')}</td>
                  <td>
                    <button
                      type="button"
                      className="button"
                      style={{
                        padding: '0.35rem 0.6rem',
                        fontSize: '0.8rem',
                        background: 'rgba(239, 68, 68, 0.9)'
                      }}
                      disabled={deleteMutation.isPending}
                      onClick={() => deleteMutation.mutate(d.id)}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            {knowledgeQuery.data && knowledgeQuery.data.length === 0 && (
              <tr>
                <td colSpan={3} className="muted">
                  No knowledge documents yet. Upload a PDF/DOCX to start.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

