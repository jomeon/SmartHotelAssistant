import { render, screen } from '@testing-library/react';
import App from './App';
import { describe, it, expect, vi } from 'vitest';

// --- POPRAWKA TUTAJ ---
// Zamiast global.fetch, używamy vi.stubGlobal - to bezpieczniejsze w Vitest
const mockFetch = vi.fn(() =>
  Promise.resolve({
    json: () => Promise.resolve([]),
  })
);

vi.stubGlobal('fetch', mockFetch);
// ---------------------

describe('App Component', () => {
  it('renders the main title', () => {
    render(<App />);
    expect(screen.getByText(/Smart Hotel Assistant/i)).toBeInTheDocument();
  });

  it('renders navigation buttons', () => {
    render(<App />);
    expect(screen.getByText(/Nowa Rezerwacja/i)).toBeInTheDocument();
  });
});