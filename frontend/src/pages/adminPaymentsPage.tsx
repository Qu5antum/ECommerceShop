import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { paymentApi } from '../api/payment'
import { userApi } from '../api/user'
import type { PaymentResponseDto, PaymentStatisticsDto, PaymentStatus } from '../types/payment'
import { PaymentStatus as PStatus } from '../types/payment'
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminPaymentsPage.css'

export default function AdminPaymentsPage() {
  const [currentUser, setCurrentUser] = useState<UserResponseDto | null>(null)
  const [payments, setPayments] = useState<PaymentResponseDto[]>([])
  const [statistics, setStatistics] = useState<PaymentStatisticsDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [selectedStatus, setSelectedStatus] = useState<string>('all')

  useEffect(() => {
    async function initAdminPayments() {
      try {
        setLoading(true)
        setError('')

        const profile = await userApi.getCurrentUserProfile()
        setCurrentUser(profile)

        const allowedRoles = UserRole.Admin | UserRole.Manager | UserRole.Moderator
        if (!profile || (profile.roles & allowedRoles) === 0) {
          setError('Access Denied: You do not have permission to view this page.')
          setLoading(false)
          return
        }

        await fetchData()
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to initialize payments page')
        setLoading(false)
      }
    }

    initAdminPayments()
  }, [])

  async function fetchData(statusFilter?: PaymentStatus) {
    try {
      setLoading(true)
      setError('')

      const [paymentsData, statsData] = await Promise.all([
        paymentApi.getPaymentsAdmin(statusFilter),
        paymentApi.getPaymentsStatistics()
      ])

      setPayments(paymentsData)
      setStatistics(statsData)
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to load payments data')
    } finally {
      setLoading(false)
    }
  }

  function handleStatusChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const val = e.target.value
    setSelectedStatus(val)
    const statusNum = val === 'all' ? undefined : (Number(val) as PaymentStatus)
    fetchData(statusNum)
  }

  function getStatusLabel(status: PaymentStatus): { label: string; className: string } {
    switch (status) {
      case PStatus.Pending:
        return { label: 'Pending', className: 'status-pending' }
      case PStatus.Succeeded:
        return { label: 'Succeeded', className: 'status-approved' }
      case PStatus.Failed:
        return { label: 'Failed', className: 'status-suspended' }
      case PStatus.Refunded:
        return { label: 'Refunded', className: 'status-processing' }
      default:
        return { label: 'Unknown', className: '' }
    }
  }

  if (loading && payments.length === 0 && !statistics) return <div className="loading">Loading payments dashboard...</div>

  if (error && error.includes('Access Denied')) {
    return (
      <main className="admin-payments-page">
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
    <main className="admin-payments-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="user/profile" className="button button--quiet">← Back to Admin Panel</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="admin-header-row">
          <h1>Payments Management</h1>
          <span className="admin-role-indicator">
            Logged in as: <strong>{currentUser?.userName}</strong>
          </span>
        </div>

        {error && <p className="form-message form-message--error">{error}</p>}

        {statistics && (
          <div className="stats-grid">
            <div className="stat-card stat-card-revenue">
              <span className="stat-label">Total Revenue</span>
              <span className="stat-value">${statistics.revenue.toFixed(2)}</span>
            </div>
            <div className="stat-card">
              <span className="stat-label">Total Transactions</span>
              <span className="stat-value">{statistics.total}</span>
            </div>
            <div className="stat-card">
              <span className="stat-label">Succeeded</span>
              <span className="stat-value text-success">{statistics.successfull}</span>
            </div>
            <div className="stat-card">
              <span className="stat-label">Pending</span>
              <span className="stat-value text-warning">{statistics.pending}</span>
            </div>
            <div className="stat-card">
              <span className="stat-label">Failed</span>
              <span className="stat-value text-danger">{statistics.failed}</span>
            </div>
            <div className="stat-card">
              <span className="stat-label">Refunded</span>
              <span className="stat-value text-info">{statistics.refunded}</span>
            </div>
          </div>
        )}

        <div className="filters-card">
          <div className="filter-field">
            <label>Filter by Payment Status</label>
            <select value={selectedStatus} onChange={handleStatusChange}>
              <option value="all">All Statuses</option>
              <option value={PStatus.Pending}>Pending</option>
              <option value={PStatus.Succeeded}>Succeeded</option>
              <option value={PStatus.Failed}>Failed</option>
              <option value={PStatus.Refunded}>Refunded</option>
            </select>
          </div>
        </div>

        <div className="payments-table-card">
          <h2>Payments History ({payments.length})</h2>

          {payments.length === 0 ? (
            <p className="no-payments">No payments found.</p>
          ) : (
            <div className="table-responsive">
              <table className="admin-payments-table">
                <thead>
                  <tr>
                    <th>Payment ID</th>
                    <th>Order ID</th>
                    <th>User ID</th>
                    <th>Amount</th>
                    <th>Status</th>
                    <th>Provider ID</th>
                    <th>Created At</th>
                  </tr>
                </thead>
                <tbody>
                  {payments.map((payment) => {
                    const statusInfo = getStatusLabel(payment.status)
                    return (
                      <tr key={payment.id}>
                        <td className="payment-id-cell">{payment.id}</td>
                        <td className="payment-order-cell">{payment.orderId}</td>
                        <td className="payment-user-cell">{payment.userId}</td>
                        <td className="payment-amount-cell">${payment.amount.toFixed(2)}</td>
                        <td>
                          <span className={`status-badge ${statusInfo.className}`}>
                            {statusInfo.label}
                          </span>
                        </td>
                        <td className="provider-id-cell">
                          {payment.providerPaymentId ? payment.providerPaymentId : '—'}
                        </td>
                        <td className="payment-date-cell">
                          {new Date(payment.createdAt).toLocaleString()}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </main>
  )
}