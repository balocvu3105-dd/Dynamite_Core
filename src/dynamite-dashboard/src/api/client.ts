import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: { 'Content-Type': 'application/json' },
})

// Attach JWT + Discord token to every request
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken')
  const discordToken = localStorage.getItem('discordToken')

  if (token) config.headers.Authorization = `Bearer ${token}`
  if (discordToken) config.headers['X-Discord-Token'] = discordToken

  return config
})

// On 401 — clear storage and redirect to login
api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.clear()
      window.location.href = '/login'
    }
    return Promise.reject(err)
  }
)

export default api
