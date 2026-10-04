import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { orderApi } from '../api/order'
import { productApi } from '../api/product'
import { OrderStatus, type OrderResponseDto } from '../types/order'
import Brand from '../components/Brand'

interface OrderWithImages extends OrderResponseDto {
  itemImages?: Record<string, string>
}

export default function OrdersPage() {
  const [orders, setOrders] = useState<OrderWithImages[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  useEffect(() => {
    async function loadOrders() {
      try {
        setLoading(true)
        const data = await orderApi.getOrdersOfUser()
        
        const ordersWithImages = await Promise.all(
          data.map(async (order) => {
            const itemImages: Record<string, string> = {}
            await Promise.all(
              order.orderItems.map(async (item) => {
                try {
                  const imgUrl = await productApi.getProductImage(item.productId)
                  if (imgUrl) {
                    itemImages[item.productId] = imgUrl
                  }
                } catch {
                }
              })
            )
            return { ...order, itemImages }
          })
        )

        setOrders(ordersWithImages)
      } catch (err: any) {
        setError(err.message || 'Failed to load orders')
      } finally {
        setLoading(false)
      }
    }

    loadOrders()
  }, [])

  async function handleCancelOrder(orderId: string) {
    if (!window.confirm('Are you sure you want to cancel this order?')) return

    try {
      setError('')
      await orderApi.cancelOrder(orderId)
      
      setOrders(orders.map(order => 
        order.id === orderId ? { ...order, status: OrderStatus.Cancelled } : order
      ))
      
      setSuccessMessage('Order successfully cancelled.')
      setTimeout(() => setSuccessMessage(''), 3000)
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to cancel order')
    }
  }

  function renderStatusBadge(status: OrderStatus) {
    switch (status) {
      case OrderStatus.Pending:
        return <span className="status-badge status-pending">Pending</span>
      case OrderStatus.PaymentPending:
        return <span className="status-badge status-payment">Payment Pending</span>
      case OrderStatus.Paid:
        return <span className="status-badge status-paid">Paid</span>
      case OrderStatus.Processing:
        return <span className="status-badge status-processing">Processing</span>
      case OrderStatus.Shipped:
        return <span className="status-badge status-shipped">Shipped</span>
      case OrderStatus.Delivered:
        return <span className="status-badge status-delivered">Delivered</span>
      case OrderStatus.Cancelled:
        return <span className="status-badge status-cancelled">Cancelled</span>
      default:
        return <span className="status-badge">Unknown</span>
    }
  }

  if (loading) return <div className="loading">Loading orders...</div>

  return (
    <main className="orders-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <div className="site-header__actions">
            <Link to="/cart" className="button button--quiet">Cart</Link>
            <Link to="/main" className="button button--quiet">← Back to Catalog</Link>
          </div>
        </div>
      </header>

      <div className="orders-container">
        <h1>My Orders</h1>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        {orders.length === 0 ? (
          <div className="empty-orders">
            <p>You haven't placed any orders yet.</p>
            <Link to="/main" className="button button--dark">Start Shopping</Link>
          </div>
        ) : (
          <div className="orders-list">
            {orders.map((order) => (
              <div key={order.id} className="order-card">
                <div className="order-header">
                  <div className="order-meta">
                    <span className="order-id">Order ID: {order.id.slice(0, 8)}...</span>
                    <span className="order-date">
                      {new Date(order.createdAt).toLocaleDateString()} {new Date(order.createdAt).toLocaleTimeString()}
                    </span>
                  </div>
                  <div className="order-status-wrapper">
                    {renderStatusBadge(order.status)}
                  </div>
                </div>

                <div className="order-items-list">
                  {order.orderItems.map((item) => (
                    <div key={item.id} className="order-item-row">
                      <div className="order-item-image-wrapper">
                        {order.itemImages?.[item.productId] ? (
                          <img 
                            src={order.itemImages[item.productId]} 
                            alt={item.productName} 
                            className="order-item-image" 
                          />
                        ) : (
                          <div className="order-item-image-placeholder">No img</div>
                        )}
                      </div>
                      <div className="order-item-info">
                        <h4>{item.productName}</h4>
                        <p>Qty: {item.quantity} × ${item.price.toFixed(2)}</p>
                      </div>
                      <div className="order-item-total">
                        ${(item.price * item.quantity).toFixed(2)}
                      </div>
                    </div>
                  ))}
                </div>

                <div className="order-footer">
                  <div className="order-total-amount">
                    <span>Total Amount:</span>
                    <strong>${order.totalAmount.toFixed(2)}</strong>
                  </div>

                  {order.status !== OrderStatus.Shipped && 
                   order.status !== OrderStatus.Delivered && 
                   order.status !== OrderStatus.Cancelled && (
                    <button 
                      type="button" 
                      className="button button--quiet button--small text-danger"
                      onClick={() => handleCancelOrder(order.id)}
                    >
                      Cancel Order
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </main>
  )
}