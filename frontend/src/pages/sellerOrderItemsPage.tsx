import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { orderApi } from '../api/order'
import type { OrderResponseWithItemsAndUser } from '../types/order'
import { OrderStatus } from '../types/order'
import Brand from '../components/Brand'

export default function SellerOrderItemsDetailPage() {
  const { id } = useParams<{ id: string }>()

  const [order, setOrder] = useState<OrderResponseWithItemsAndUser | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!id) return

    async function fetchOrderDetail() {
      try {
        setLoading(true)
        setError('')
        const data = await orderApi.getOrderWithItemsAndUser(id!)
        setOrder(data)
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load order details')
      } finally {
        setLoading(false)
      }
    }

    fetchOrderDetail()
  }, [id])

  function renderOrderStatus(status: number) {
    switch (status) {
      case OrderStatus.Pending:
        return <span className="status-badge status-pending">Pending</span>
      case OrderStatus.PaymentPending:
        return <span className="status-badge status-pending">Payment Pending</span>
      case OrderStatus.Paid:
        return <span className="status-badge status-approved">Paid</span>
      case OrderStatus.Processing:
        return <span className="status-badge status-pending">Processing</span>
      case OrderStatus.Shipped:
        return <span className="status-badge status-approved">Shipped</span>
      case OrderStatus.Delivered:
        return <span className="status-badge status-completed">Delivered</span>
      case OrderStatus.Cancelled:
        return <span className="status-badge status-cancelled">Cancelled</span>
      default:
        return <span className="status-badge status-suspended">Unknown</span>
    }
  }

  if (loading) return <div className="loading">Loading order details...</div>
  if (error) return <div className="error-container"><h2>Error</h2><p>{error}</p><Link to="/seller/profile">Back to Dashboard</Link></div>
  if (!order) return <div className="loading">Order not found</div>

  return (
    <main className="seller-order-detail-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/seller/profile" className="button button--quiet">← Back to Dashboard</Link>
        </div>
      </header>

      <div className="order-detail-container">
        <div className="order-header-row">
          <div>
            <h1>Order Details</h1>
            <span className="order-id-subtitle">ID: {order.id}</span>
          </div>
          <div className="order-status-wrapper">
            {renderOrderStatus(order.status)}
          </div>
        </div>

        <div className="order-grid-layout">
          <section className="order-items-section">
            <h2>Items in Order ({order.orderItems.length})</h2>
            <div className="items-table-wrapper">
              <table className="items-table">
                <thead>
                  <tr>
                    <th>Product</th>
                    <th>Price</th>
                    <th>Quantity</th>
                    <th className="text-right">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {order.orderItems.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <Link to={`/product/${item.productId}`} className="item-product-link">
                          {item.productName}
                        </Link>
                      </td>
                      <td>${item.price.toFixed(2)}</td>
                      <td>{item.quantity}</td>
                      <td className="text-right font-weight-bold">
                        ${(item.price * item.quantity).toFixed(2)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="order-total-summary">
              <span>Total Amount:</span>
              <span className="total-amount-value">${order.totalAmount.toFixed(2)}</span>
            </div>
          </section>

          <aside className="order-sidebar">
            <div className="sidebar-card">
              <h3>Customer Information</h3>
              {order.user ? (
                <div className="customer-info">
                  <p><strong>Name:</strong> {order.user.userName}</p>
                  <p><strong>Email:</strong> <a href={`mailto:${order.user.email}`}>{order.user.email}</a></p>
                </div>
              ) : (
                <p className="no-data">Customer details not available.</p>
              )}
            </div>

            <div className="sidebar-card">
              <h3>Order Timelines</h3>
              <div className="timeline-info">
                <p><strong>Created At:</strong> {new Date(order.createdAt).toLocaleString()}</p>
                {order.updatedAt && (
                  <p><strong>Last Updated:</strong> {new Date(order.updatedAt).toLocaleString()}</p>
                )}
              </div>
            </div>
          </aside>
        </div>
      </div>
    </main>
  )
}