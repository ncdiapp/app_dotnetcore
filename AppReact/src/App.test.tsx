import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';

test('renders app title', () => {
  render(<App />);
  const titleElement = screen.getByText(/React \+ TypeScript \+ Tailwind CSS/i);
  expect(titleElement).toBeInTheDocument();
});

test('renders welcome message', () => {
  render(<App />);
  const messageElement = screen.getByText(/Your new app is ready!/i);
  expect(messageElement).toBeInTheDocument();
});
