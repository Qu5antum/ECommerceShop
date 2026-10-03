import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import axios from 'axios'
import { authApi } from '../api/auth'
import type { LoginRequest, RegisterRequest } from '../types/auth'
import Brand from '../components/Brand'

type AuthMode = 'login' | 'register'

interface AuthPageProps {
  mode: AuthMode
}

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError<{ detail?: string; message?: string; title?: string }>(error)) {
    return (
      error.response?.data?.detail ??
      error.response?.data?.message ??
      error.response?.data?.title ??
      'Failed to complete the request. Check your connection and try again.'
    )
  }
  return error instanceof Error ? error.message : 'An unexpected error occurred. Please try again.'
}

export default function AuthPage({ mode }: AuthPageProps) {
  const isRegister = mode === 'register'
  const navigate = useNavigate() 

  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setErrorMessage('')
    setSuccessMessage('')

    const formData = new FormData(event.currentTarget)
    const email = String(formData.get('email') ?? '').trim()
    const password = String(formData.get('password') ?? '')

    if (isRegister) {
      const userName = String(formData.get('userName') ?? '').trim()
      const passwordConfirmation = String(formData.get('passwordConfirmation') ?? '')

      if (password !== passwordConfirmation) {
        setErrorMessage('Passwords do not match. Check both fields.')
        return
      }

      const registration: RegisterRequest = { UserName: userName, Email: email, Password: password }
      setIsSubmitting(true)
      try {
        await authApi.register(registration)
        setSuccessMessage('Account created successfully. Redirecting...')
        
        setTimeout(() => {
          navigate('/login')
        }, 1500)
      } catch (error: unknown) {
        setErrorMessage(getErrorMessage(error))
      } finally {
        setIsSubmitting(false)
      }
      return
    }

    const credentials: LoginRequest = { Email: email, Password: password }
    setIsSubmitting(true)
    try {
      const response = await authApi.login(credentials)
      if (!response.accessToken) {
        throw new Error('Server did not return an access token. Please try logging in again.')
      }
      localStorage.setItem('access_token', response.accessToken)
      
      navigate('/main')

    } catch (error: unknown) {
      setErrorMessage(getErrorMessage(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-page">
      <Link to="/main" className="auth-back" aria-label="Return to home page">
        <span aria-hidden="true">←</span> Home
      </Link>
      <section className="auth-card" aria-labelledby="auth-title">
        <Brand />
        <div className="auth-card__heading">
          <span className="eyebrow">{isRegister ? 'Welcome to Lavka' : 'Welcome back'}</span>
          <h1 id="auth-title">{isRegister ? 'Create an account' : 'Glad to see you'}</h1>
          <p>{isRegister ? 'Fill out a few fields — and you are good to go.' : 'Enter your account details to continue.'}</p>
        </div>

        <form className="auth-form" onSubmit={handleSubmit}>
          {isRegister && (
            <label className="form-field">
              <span>Username</span>
              <input autoComplete="username" name="userName" placeholder="What should we call you?" required minLength={2} maxLength={64} />
            </label>
          )}
          <label className="form-field">
            <span>Email</span>
            <input autoComplete="email" name="email" placeholder="name@example.com" type="email" required />
          </label>
          <label className="form-field">
            <span>Password</span>
            <input autoComplete={isRegister ? 'new-password' : 'current-password'} name="password" type="password" required />
          </label>
          {isRegister && (
            <label className="form-field">
              <span>Confirm password</span>
              <input autoComplete="new-password" name="passwordConfirmation" placeholder="Enter password again" type="password" required />
            </label>
          )}

          {errorMessage && <p className="form-message form-message--error" role="alert">{errorMessage}</p>}
          {successMessage && <p className="form-message form-message--success" role="status">{successMessage}</p>}

          <button className="button button--dark button--submit" disabled={isSubmitting} type="submit">
            {isSubmitting ? 'Please wait…' : isRegister ? 'Create account' : 'Log in'}
            {!isSubmitting && <span aria-hidden="true">↗</span>}
          </button>
        </form>

        <p className="auth-switch">
          {isRegister ? 'Already have an account?' : 'New to Lavka?'}{' '}
          <Link to={isRegister ? '/login' : '/register'}>
            {isRegister ? 'Log in' : 'Create account'}
          </Link>
        </p>
        <p className="auth-legal">By continuing, you agree to the store terms of use.</p>
      </section>
      <div className="auth-decoration" aria-hidden="true">
        <span>✳</span><span>✦</span><span>♡</span>
      </div>
    </main>
  )
}