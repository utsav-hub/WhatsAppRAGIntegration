import axios from 'axios';

export const api = axios.create({
  baseURL: 'http://localhost:5000/', // adjust if your HTTPS port differs
  timeout: 10000
});

