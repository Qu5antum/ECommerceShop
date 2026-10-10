import { useEffect, useState, type FormEvent } from 'react'
import { useParams, Link } from 'react-router-dom'
import { paymentApi } from '../api/payment'
import { userApi } from '../api/user'
import type { PaymentResponseDto, PaymentWebhookDto } from '../types/payment'
import { PaymentStatus } from '../types/payment'
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminPaymentDetailPage.css'

export default function AdminPaymentDetailPage() {
  const { id } = useParams<{ id: string }>()

  const [payment, setPayment] = useState<PaymentResponseDto | null>(null)
  const [currentUser, setCurrentUser] = useState<UserResponseDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)

  // Поля для формы отправки вебхука
  const [webhookStatus, setWebhookStatus] = useState<PaymentStatus>(PaymentStatus.Succeeded)
  const [providerIdInput, setProviderIdInput] = useState('')

  useEffect(() => {
    async function initPaymentDetail() {
      if (!id) return
      try {
        setLoading(true)
        setError('')

        // 1. Проверяем права доступа (Admin, Manager, Moderator)
        const profile = await userApi.getCurrentUserProfile()
        setCurrentUser(profile)

        const allowedRoles = UserRole.Admin | UserRole.Manager | UserRole.Moderator
        if (!profile || (profile.roles & allowedRoles) === 0) {
          setError('Access Denied: You do not have permission to view this page.')
          setLoading(false)
          return
        }

        // 2. Получаем данные конкретного платежа
        const paymentData = await paymentApi.getPaymentAdmin(id)
        setPayment(paymentData)
        setWebhookStatus(paymentData.status)
        setProviderIdInput(paymentData.providerPaymentId || '')
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load payment details')
      } finally {
        setLoading(false)
      }
    }

    initPaymentDetail()
  }, [id])

  async function handleWebhookSubmit(e: FormEvent) {
    e.preventDefault()
    if (!payment) return

    const targetProviderId = providerIdInput.trim() || payment.providerPaymentId
    if (!targetProviderId) {
      setError('Provider Payment ID is required for webhook processing.')
      return
    }

    try {
      setSubmitting(true)
      setError('')
      setSuccessMessage('')

      const webhookDto: PaymentWebhookDto = {
        providerPaymentId: targetProviderId,
        status: Number(webhookStatus) as PaymentStatus
      }

      const res = await paymentApi.processWebhook(webhookDto)

      // Обновляем локальное состояние
      setPayment({
        ...payment,
        status: Number(webhookStatus) as PaymentStatus,
        providerPaymentId: targetProviderId,
        updatedAt: new Date().toISOString()
      })

      setSuccessMessage(res.message || 'Webhook successfully processed!')
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to process payment webhook')
    } finally {
      setSubmitting(false)
    }
  }

  function getStatusLabel(status: PaymentStatus): { label: string; className: string } {
    switch (status) {
      case PaymentStatus.Pending:
        return { label: 'Pending', className: 'status-pending' }
      case PaymentStatus.Succeeded:
        return { label: 'Succeeded', className: 'status-approved' }
      case PaymentStatus.Failed:
        return { label: 'Failed', className: 'status-suspended' }
      case PaymentStatus.Refunded:
        return { label: 'Refunded', className: 'status-processing' }
      default:
        return { label: 'Unknown', className: '' }
    }
  }

  if (loading) return <div className="loading">Loading payment details...</div>

  if (error && error.includes('Access Denied')) {
    return (
      <main className="admin-payment-detail-page">
        <header className="site-header">
          <div className="site-header__inner">
            <Brand />
            <Link to="/main" className="button button--quiet">← Back to Catalog</Link>
          </div>
        </header>
        <div className="admin-container">
          <div className="access-denied-card">
            <h2>Access Denied</h2>
            <p>{error}</p>
            <Link to="/main" className="button button--dark admin-return-btn">
              Return to Catalog
            </Link>
          </div>
        </div>
      </main>
    )
  }

  return (
    <main className="admin-payment-detail-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/admin/payments" className="button button--quiet">← Back to Payments List</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="detail-header-row">
          <div>
            <h1>Payment Management</h1>
            <span className="payment-id-subtitle">ID: {payment?.id}</span>
          </div>
          <span className="admin-role-indicator">
            Logged in as: <strong>{currentUser?.userName}</strong>
          </span>
        </div>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        {payment && (
          <div className="payment-detail-grid">
            {/* Левая колонка: Детали платежа */}
            <div className="detail-card">
              <h2>Payment Specifications</h2>
              <div className="detail-info-list">
                <div className="detail-row">
                  <span className="label">Order ID:</span>
                  <Link to={`/admin/orders/${payment.orderId}`} className="value code-text link-text">
                    {payment.orderId} →
                  </Link>
                </div>
                <div className="detail-row">
                  <span className="label">User ID:</span>
                  <span className="value code-text">{payment.userId}</span>
                </div>
                <div className="detail-row">
                  <span className="label">Amount:</span>
                  <span className="value font-weight-bold payment-amount">
                    ${payment.amount.toFixed(2)}
                  </span>
                </div>
                <div className="detail-row">
                  <span className="label">Current Status:</span>
                  <span className={`status-badge ${getStatusLabel(payment.status).className}`}>
                    {getStatusLabel(payment.status).label}
                  </span>
                </div>
                <div className="detail-row">
                  <span className="label">Provider Payment ID:</span>
                  <span className="value code-text">{payment.providerPaymentId || '—'}</span>
                </div>
                <div className="detail-row">
                  <span className="label">Created At:</span>
                  <span className="value">{new Date(payment.createdAt).toLocaleString()}</span>
                </div>
                {payment.updatedAt && (
                  <div className="detail-row">
                    <span className="label">Last Updated:</span>
                    <span className="value">{new Date(payment.updatedAt).toLocaleString()}</span>
                  </div>
                )}
              </div>
            </div>

            {/* Правая колонка: Симуляция / Обработка Webhook */}
            <div className="detail-card">
              <h2>Process Payment Webhook</h2>
              <p className="webhook-description">
                Simulate or send a provider webhook notification to manually update the payment status and synchronize transactions.
              </p>

              <form onSubmit={handleWebhookSubmit} className="webhook-form">
                <label className="form-field">
                  <span>Provider Payment ID *</span>
                  <input 
                    type="text" 
                    value={providerIdInput} 
                    onChange={(e) => setProviderIdInput(e.target.value)} 
                    placeholder="e.g. pay_123456789"
                    required 
                  />
                </label>

                <label className="form-field">
                  <span>Target Status *</span>
                  <select 
                    value={webhookStatus} 
                    onChange={(e) => setWebhookStatus(Number(e.target.value) as PaymentStatus)}
                    className="webhook-select"
                  >
                    <option value={PaymentStatus.Pending}>Pending</option>
                    <option value={PaymentStatus.Succeeded}>Succeeded</option>
                    <option value={PaymentStatus.Failed}>Failed</option>
                    <option value={PaymentStatus.Refunded}>Refunded</option>
                  </select>
                </label>

                <div className="form-actions" style={{ marginTop: '16px' }}>
                  <button type="submit" className="button button--dark" disabled={submitting}>
                    {submitting ? 'Processing...' : 'Send Webhook Update'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </main>
  )
}