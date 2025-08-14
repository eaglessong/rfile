import React from 'react';
import { render } from '@testing-library/react';
import App from './App';

test('renders app without crashing', () => {
  // The app should render without throwing an error
  render(<App />);
  // If we get here without an exception, the test passes
  expect(true).toBe(true);
});
