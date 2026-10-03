import MainPage from './pages/mainPage'
import LoginPage from './pages/loginPage'
import RegisterPage from './pages/registerPage'

function App() {
  const path = window.location.pathname.replace(/\/+$/, '') || '/'

  if (path === '/login') {
    return <LoginPage />
  }

  if (path === '/register') {
    return <RegisterPage />
  }

  return <MainPage />
}

export default App
