import { NavLink, Route, Routes } from 'react-router-dom';
import { DashboardPage } from './pages/DashboardPage';
import { ConversationsPage } from './pages/ConversationsPage';
import { LeadsPage } from './pages/LeadsPage';
import { KeywordsPage } from './pages/KeywordsPage';
import { KnowledgePage } from './pages/KnowledgePage';

export function App() {
  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="logo">Octology Admin</div>
        <nav>
          <NavLink to="/" end className="nav-link">
            Dashboard
          </NavLink>
          <NavLink to="/conversations" className="nav-link">
            Conversations
          </NavLink>
          <NavLink to="/leads" className="nav-link">
            Leads
          </NavLink>
          <NavLink to="/keywords" className="nav-link">
            Keywords
          </NavLink>
          <NavLink to="/knowledge" className="nav-link">
            Knowledge
          </NavLink>
        </nav>
      </aside>
      <main className="main">
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/conversations" element={<ConversationsPage />} />
          <Route path="/leads" element={<LeadsPage />} />
          <Route path="/keywords" element={<KeywordsPage />} />
          <Route path="/knowledge" element={<KnowledgePage />} />
        </Routes>
      </main>
    </div>
  );
}

