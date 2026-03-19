import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { api } from '../api';

type AdminSummaryDto = {
  date: string;
  newUsers: number;
  totalUsers: number;
  conversationsToday: number;
  leadsToday: number;
  totalLeads: number;
};

type AdminConversationInsightsDto = {
  date: string;
  totalConversations: number;
  greetingCount: number;
  freightQuoteCount: number;
  otherCount: number;
};

export function DashboardPage() {
  const today = dayjs().format('YYYY-MM-DD');

  const {
    data: summary,
    isLoading: summaryLoading,
    isError: summaryError
  } = useQuery({
    queryKey: ['summary', today],
    queryFn: async () => {
      const res = await api.get<AdminSummaryDto>('/api/admin/summary', {
        params: { date: today }
      });
      return res.data;
    }
  });

  const {
    data: insights,
    isLoading: insightsLoading,
    isError: insightsError
  } = useQuery({
    queryKey: ['conversation-insights', today],
    queryFn: async () => {
      const res = await api.get<AdminConversationInsightsDto>('/api/admin/insights/conversations', {
        params: { date: today }
      });
      return res.data;
    }
  });

  if (summaryLoading || insightsLoading) {
    return <div>Loading dashboard...</div>;
  }

  if (summaryError || !summary || insightsError || !insights) {
    return <div>Failed to load dashboard summary.</div>;
  }

  const total = insights.totalConversations || 1;
  const greetPct = Math.round((insights.greetingCount / total) * 100);
  const freightPct = Math.round((insights.freightQuoteCount / total) * 100);
  const otherPct = Math.round((insights.otherCount / total) * 100);

  return (
    <div>
      <h1>Dashboard</h1>
      <p className="muted">Snapshot for {dayjs(summary.date).format('YYYY-MM-DD')}</p>
      <div className="card-grid">
        <div className="card-row">
          <div className="card">
            <div className="card-title">New users today</div>
            <div className="card-value">{summary.newUsers}</div>
          </div>
          <div className="card">
            <div className="card-title">Total users</div>
            <div className="card-value">{summary.totalUsers}</div>
          </div>
          <div className="card">
            <div className="card-title">Conversations today</div>
            <div className="card-value">{summary.conversationsToday}</div>
          </div>
          <div className="card">
            <div className="card-title">Leads today</div>
            <div className="card-value">{summary.leadsToday}</div>
          </div>
          <div className="card">
            <div className="card-title">Total leads</div>
            <div className="card-value">{summary.totalLeads}</div>
          </div>
        </div>

        <div className="card-row wide">
          <div className="card">
            <div className="card-title">Query mix today</div>
            <div className="muted" style={{ fontSize: '0.8rem', marginBottom: '0.4rem' }}>
              Based on user messages
            </div>
            <div className="insights-bars">
              <div className="insight-row">
                <span>Greetings</span>
                <span>
                  {insights.greetingCount} ({greetPct}%)
                </span>
              </div>
              <div className="bar">
                <span style={{ width: `${greetPct}%` }} className="bar-fill greet" />
              </div>

              <div className="insight-row">
                <span>Freight / tracking</span>
                <span>
                  {insights.freightQuoteCount} ({freightPct}%)
                </span>
              </div>
              <div className="bar">
                <span style={{ width: `${freightPct}%` }} className="bar-fill freight" />
              </div>

              <div className="insight-row">
                <span>Other</span>
                <span>
                  {insights.otherCount} ({otherPct}%)
                </span>
              </div>
              <div className="bar">
                <span style={{ width: `${otherPct}%` }} className="bar-fill other" />
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-title">Key customers (sample)</div>
            <div className="muted" style={{ fontSize: '0.8rem', marginBottom: '0.4rem' }}>
              Mocked data for dashboard layout
            </div>
            <div className="table-wrapper">
              <table className="table">
                <thead>
                  <tr>
                    <th>Customer name</th>
                    <th>WhatsApp number</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td>Acme Logistics Pvt Ltd</td>
                    <td>+91 90000 11111</td>
                  </tr>
                  <tr>
                    <td>Global Freight Movers</td>
                    <td>+91 90000 22222</td>
                  </tr>
                  <tr>
                    <td>Oceanic Imports &amp; Exports</td>
                    <td>+91 90000 33333</td>
                  </tr>
                  <tr>
                    <td>Skyline Retail Distribution</td>
                    <td>+91 90000 44444</td>
                  </tr>
                  <tr>
                    <td>Vertex Manufacturing</td>
                    <td>+91 90000 55555</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

