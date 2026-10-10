import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { orderApi } from '../api/order'
import { userApi } from '../api/user'
import type { OrderResponseDto, UpdateOrderStatusDto } from '../types/order'
import { OrderStatus } from '../types/order'
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminOrderDetailPage.css'

export default function AdminOrderDetailPage() {
  const { id } = useParams<{ id: string }>()

  const [order, setOrder] = useState<OrderResponseDto | null>(null)
  const [currentUser, setCurrentUser] = useState<UserResponseDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [selectedStatus, setSelectedStatus] = useState<OrderStatus | ''>('')

  useEffect(() => {
    async function initOrderDetail() {
      if (!id) return
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

        const orderData = await orderApi.getOrderAdmin(id)
        setOrder(orderData)
        setSelectedStatus(orderData.status)
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load order details')
      } finally {
        setLoading(false)
      }
    }

    initOrderDetail()
  }, [id])

  async function handleStatusUpdate(e: React.FormEvent) {
    e.preventDefault()
    if (!id || !order || selectedStatus === '') return

    try {
      setSubmitting(true)
      setError('')
      setSuccessMessage('')

      const updateDto: UpdateOrderStatusDto = {
        status: Number(selectedStatus) as OrderStatus
      }

      await orderApi.updateStatusOfOrder(id, updateDto)

      setOrder({
        ...order,
        status: Number(selectedStatus) as OrderStatus,
        updatedAt: new Date().toISOString()
      })

      setSuccessMessage('Order status successfully updated!')
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to update order status')
    } finally {
      setSubmitting(false)
    }
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

  if (loading) return <div className="loading">Loading order details...</div>

  if (error && error.includes('Access Denied')) {
    return (
      <main className="admin-order-detail-page">
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
    <main className="admin-order-detail-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/admin/orders" className="button button--quiet">← Back to Orders List</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="detail-header-row">
          <div>
            <h1>Order Management</h1>
            <span className="order-id-subtitle">ID: {order?.id}</span>
          </div>
          <span className="admin-role-indicator">
            Logged in as: <strong>{currentUser?.userName}</strong>
          </span>
        </div>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        {order && (
          <div className="order-detail-grid">
            <div className="detail-card">
              <h2>Order Information</h2>
              <div className="detail-info-list">
                <div className="detail-row">
                  <span className="label">User ID:</span>
                  <span className="value code-text">{order.userId}</span>
                </div>
                <div className="detail-row">
                  <span className="label">Current Status:</span>
                  <span className={`status-badge ${getStatusLabel(order.status).className}`}>
                    {getStatusLabel(order.status).label}
                  </span>
                </div>
                <div className="detail-row">
                  <span className="label">Total Amount:</span>
                  <span className="value font-weight-bold order-total-price">
                    ${order.totalAmount.toFixed(2)}
                  </span>
                </div>
                <div className="detail-row">
                  <span className="label">Created At:</span>
                  <span className="value">{new Date(order.createdAt).toLocaleString()}</span>
                </div>
                {order.updatedAt && (
                  <div className="detail-row">
                    <span className="label">Last Updated:</span>
                    <span className="value">{new Date(order.updatedAt).toLocaleString()}</span>
                  </div>
                )}
              </div>

              <div className="status-update-section">
                <h3>Update Order Status</h3>
                <form onSubmit={handleStatusUpdate} className="status-form">
                  <select 
                    value={selectedStatus} 
                    onChange={(e) => setSelectedStatus(Number(e.target.value) as OrderStatus)}
                    className="status-select"
                  >
                    <option value={OrderStatus.Pending}>Pending</option>
                    <option value={OrderStatus.PaymentPending}>Payment Pending</option>
                    <option value={OrderStatus.Paid}>Paid</option>
                    <option value={OrderStatus.Processing}>Processing</option>
                    <option value={OrderStatus.Shipped}>Shipped</option>
                    <option value={OrderStatus.Delivered}>Delivered</option>
                    <option value={OrderStatus.Cancelled}>Cancelled</option>
                  </select>

                  <button type="submit" className="button button--dark" disabled={submitting}>
                    {submitting ? 'Updating...' : 'Save Status'}
                  </button>
                </form>
              </div>
            </div>

            <div className="detail-card">
              <h2>Order Items ({order.orderItems?.length || 0})</h2>

              {(!order.orderItems || order.orderItems.length === 0) ? (
                <p className="no-items">No items in this order.</p>
              ) : (
                <div className="table-responsive">
                  <table className="admin-order-items-table">
                    <thead>
                      <tr>
                        <th>Product Name</th>
                        <th>Price</th>
                        <th>Quantity</th>
                        <th>Subtotal</th>
                      </tr>
                    </thead>
                    <tbody>
                      {order.orderItems.map((item) => (
                        <tr key={item.id}>
                          <td className="font-weight-bold">{item.productName}</td>
                          <td>${item.price.toFixed(2)}</td>
                          <td>{item.quantity}</td>
                          <td className="font-weight-bold">${(item.price * item.quantity).toFixed(2)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </main>
  )
}