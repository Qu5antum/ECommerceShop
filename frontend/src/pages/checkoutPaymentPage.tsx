import { useEffect, useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { paymentApi } from '../api/payment'
import { PaymentStatus, type PaymentResponseDto } from '../types/payment'
import Brand from '../components/Brand'

export default function CheckoutPaymentPage() {
  const { orderId } = useParams<{ orderId: string }>()
  const navigate = useNavigate()

  const [payment, setPayment] = useState<PaymentResponseDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  useEffect(() => {
    async function initPayment() {
      if (!orderId) return
      try {
        setLoading(true)
        const paymentData = await paymentApi.createPayment(orderId)
        setPayment(paymentData)
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to initialize payment')
      } finally {
        setLoading(false)
      }
    }

    initPayment()
  }, [orderId])

  async function handleCheckStatus() {
    if (!orderId || !payment) return
    try {
      setError('')
      setLoading(true)
      const updatedPayment = await paymentApi.getPayment(orderId, payment.id)
      setPayment(updatedPayment)
      setSuccessMessage('Payment status updated.')
      setTimeout(() => setSuccessMessage(''), 3000)
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to check payment status')
    } finally {
      setLoading(false)
    }
  }

  function renderPaymentStatusBadge(status: PaymentStatus) {
    switch (status) {
      case PaymentStatus.Pending:
        return <span className="status-badge status-pending">Pending</span>
      case PaymentStatus.Succeeded:
        return <span className="status-badge status-paid">Succeeded</span>
      case PaymentStatus.Failed:
        return <span className="status-badge status-cancelled">Failed</span>
      case PaymentStatus.Refunded:
        return <span className="status-badge status-payment">Refunded</span>
      default:
        return <span className="status-badge">Unknown</span>
    }
  }

  if (loading && !payment) return <div className="loading">Initializing payment...</div>

  if (error && !payment) {
    return (
      <div className="payment-error-container">
        <p className="form-message form-message--error">{error}</p>
        <Link to={`/orders/${orderId}`} className="button button--dark">View Order</Link>
      </div>
    )
  }

  return (
    <main className="payment-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <div className="site-header__actions">
            <Link to={`/orders/${orderId}`} className="button button--quiet">← Back to Order</Link>
          </div>
        </div>
      </header>

      <div className="payment-container">
        <div className="payment-card">
          <h1>Order Checkout & Payment</h1>

          {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
          {error && <p className="form-message form-message--error">{error}</p>}

          {payment && (
            <div className="payment-details">
              <div className="payment-row">
                <span className="payment-label">Payment ID:</span>
                <span className="payment-value">{payment.id}</span>
              </div>
              <div className="payment-row">
                <span className="payment-label">Order ID:</span>
                <span className="payment-value">{payment.orderId}</span>
              </div>
              <div className="payment-row">
                <span className="payment-label">Status:</span>
                <div className="payment-value">{renderPaymentStatusBadge(payment.status)}</div>
              </div>
              <div className="payment-row">
                <span className="payment-label">Amount to Pay:</span>
                <span className="payment-amount">${payment.amount.toFixed(2)}</span>
              </div>
              {payment.providerPaymentId && (
                <div className="payment-row">
                  <span className="payment-label">Provider Ref:</span>
                  <span className="payment-value">{payment.providerPaymentId}</span>
                </div>
              )}
            </div>
          )}

          <div className="payment-actions">
            {payment?.status === PaymentStatus.Succeeded ? (
              <div className="payment-success-box">
                <p className="success-text">🎉 Payment completed successfully!</p>
                <button 
                  type="button" 
                  className="button button--dark"
                  onClick={() => navigate(`/orders/${orderId}`)}
                >
                  View Order Details
                </button>
              </div>
            ) : (
              <div className="payment-action-buttons">
                <button 
                  type="button" 
                  className="button button--dark button--full"
                  onClick={handleCheckStatus}
                  disabled={loading}
                >
                  {loading ? 'Checking...' : 'Check Payment Status'}
                </button>
                
                <p className="payment-hint">
                  After completing the payment on your gateway, click the button above to verify the transaction status.
                </p>
              </div>
            )}
          </div>
        </div>
      </div>
    </main>
  )
}