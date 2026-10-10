import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { orderApi } from '../api/order'
import { userApi } from '../api/user'
import type { OrderResponseDto } from '../types/order'
import { OrderStatus } from '../types/order'
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminOrdersPage.css'

export default function AdminOrdersPage() {
  const navigate = useNavigate() 
  const [currentUser, setCurrentUser] = useState<UserResponseDto | null>(null)
  const [orders, setOrders] = useState<OrderResponseDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [selectedStatus, setSelectedStatus] = useState<string>('all')
  const [fromDate, setFromDate] = useState<string>('')
  const [toDate, setToDate] = useState<string>('')

  useEffect(() => {
    async function initAdminOrders() {
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

        await fetchOrders()
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to initialize orders page')
        setLoading(false)
      }
    }

    initAdminOrders()
  }, [])

  async function fetchOrders(statusFilter?: OrderStatus, start?: string, end?: string) {
    try {
      setLoading(true)
      setError('')

      let data: OrderResponseDto[] = []

      if (start && end) {
        data = await orderApi.getOrdersFromDateToDateAdmin(start, end)
        if (statusFilter !== undefined && !isNaN(statusFilter)) {
          data = data.filter((o) => o.status === statusFilter)
        }
      } else {
        data = await orderApi.getOrdersAdmin(statusFilter)
      }

      setOrders(data)
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to load orders')
    } finally {
      setLoading(false)
    }
  }

  function handleStatusChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const val = e.target.value
    setSelectedStatus(val)
    const statusNum = val === 'all' ? undefined : (Number(val) as OrderStatus)
    fetchOrders(statusNum, fromDate, toDate)
  }

  function handleDateFilterSubmit(e: React.FormEvent) {
    e.preventDefault()
    const statusNum = selectedStatus === 'all' ? undefined : (Number(selectedStatus) as OrderStatus)
    fetchOrders(statusNum, fromDate, toDate)
  }

  function handleResetFilters() {
    setSelectedStatus('all')
    setFromDate('')
    setToDate('')
    fetchOrders()
  }

  function getStatusLabel(status: OrderStatus): { label: string; className: string } {
    switch (status) {
      case OrderStatus.Pending:
        return { label: 'Pending', className: 'status-pending' }
      case OrderStatus.PaymentPending:
        return { label: 'Payment Pending', className: 'status-pending' }
      case OrderStatus.Paid:
        return { label: 'Paid', className: 'status-approved' }
      case OrderStatus.Processing:
        return { label: 'Processing', className: 'status-processing' }
      case OrderStatus.Shipped:
        return { label: 'Shipped', className: 'status-shipped' }
      case OrderStatus.Delivered:
        return { label: 'Delivered', className: 'status-approved' }
      case OrderStatus.Cancelled:
        return { label: 'Cancelled', className: 'status-suspended' }
      default:
        return { label: 'Unknown', className: '' }
    }
  }

  if (loading && orders.length === 0) return <div className="loading">Loading orders...</div>

  if (error && error.includes('Access Denied')) {
    return (
      <main className="admin-orders-page">
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
    <main className="admin-orders-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/user/profile" className="button button--quiet">← Back to Admin Panel</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="admin-header-row">
          <h1>Orders Management</h1>
          <span className="admin-role-indicator">
            Logged in as: <strong>{currentUser?.userName}</strong>
          </span>
        </div>

        <div className="filters-card">
          <form onSubmit={handleDateFilterSubmit} className="filters-grid">
            <div className="filter-field">
              <label>Filter by Status</label>
              <select value={selectedStatus} onChange={handleStatusChange}>
                <option value="all">All Statuses</option>
                <option value={OrderStatus.Pending}>Pending</option>
                <option value={OrderStatus.PaymentPending}>Payment Pending</option>
                <option value={OrderStatus.Paid}>Paid</option>
                <option value={OrderStatus.Processing}>Processing</option>
                <option value={OrderStatus.Shipped}>Shipped</option>
                <option value={OrderStatus.Delivered}>Delivered</option>
                <option value={OrderStatus.Cancelled}>Cancelled</option>
              </select>
            </div>

            <div className="filter-field">
              <label>From Date</label>
              <input 
                type="date" 
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
              />
            </div>

            <div className="filter-field">
              <label>To Date</label>
              <input 
                type="date" 
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
              />
            </div>

            <div className="filter-actions">
              <button type="submit" className="button button--dark">
                Apply Dates
              </button>
              {(fromDate || toDate || selectedStatus !== 'all') && (
                <button type="button" className="button button--quiet" onClick={handleResetFilters}>
                  Reset
                </button>
              )}
            </div>
          </form>
        </div>

        {error && <p className="form-message form-message--error">{error}</p>}

        <div className="orders-table-card">
          <h2>All Orders ({orders.length})</h2>

          {orders.length === 0 ? (
            <p className="no-orders">No orders found.</p>
          ) : (
            <div className="table-responsive">
              <table className="admin-orders-table">
                <thead>
                  <tr>
                    <th>Order ID</th>
                    <th>User ID</th>
                    <th>Total Amount</th>
                    <th>Status</th>
                    <th>Created At</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {orders.map((order) => {
                    const statusInfo = getStatusLabel(order.status)
                    return (
                      <tr 
                        key={order.id} 
                        className="order-row-clickable" 
                        onClick={() => navigate(`/admin/orders/${order.id}`)} 
                      >
                        <td className="order-id-cell">{order.id}</td>
                        <td className="order-user-id-cell">{order.userId}</td>
                        <td className="order-amount-cell">${order.totalAmount.toFixed(2)}</td>
                        <td>
                          <span className={`status-badge ${statusInfo.className}`}>
                            {statusInfo.label}
                          </span>
                        </td>
                        <td className="order-date-cell">{new Date(order.createdAt).toLocaleString()}</td>
                        <td>
                          <Link 
                            to={`/admin/orders/${order.id}`} 
                            className="button button--quiet"
                            onClick={(e) => e.stopPropagation()}
                          >
                            Details →
                          </Link>
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