import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { orderApi } from '../api/order'
import { productApi } from '../api/product'
import { OrderStatus, type OrderResponseDto } from '../types/order'
import Brand from '../components/Brand'

interface OrderItemWithImage extends OrderResponseDto {
  itemImages?: Record<string, string> 
}

export default function OrderDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [order, setOrder] = useState<OrderItemWithImage | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  useEffect(() => {
    async function loadOrderDetails() {
      if (!id) return
      try {
        setLoading(true)
        const orderData = await orderApi.getOrderOfUser(id)

        const itemImages: Record<string, string> = {}
        await Promise.all(
          orderData.orderItems.map(async (item) => {
            try {
              const imgUrl = await productApi.getProductImage(item.productId)
              if (imgUrl) {
                itemImages[item.productId] = imgUrl
              }
            } catch {
            }
          })
        )

        setOrder({ ...orderData, itemImages })
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load order details')
      } finally {
        setLoading(false)
      }
    }

    loadOrderDetails()
  }, [id])

  async function handleCancelOrder() {
    if (!id || !window.confirm('Are you sure you want to cancel this order?')) return

    try {
      setError('')
      await orderApi.cancelOrder(id)
      
      setOrder(prev => prev ? { ...prev, status: OrderStatus.Cancelled } : null)
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

  if (loading) return <div className="loading">Loading order details...</div>

  if (error && !order) {
    return (
      <div className="order-error-container">
        <p className="form-message form-message--error">{error}</p>
        <Link to="/orders" className="button button--dark">Back to Orders</Link>
      </div>
    )
  }

  if (!order) return <div className="loading">Order not found.</div>

  return (
    <main className="order-detail-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <div className="site-header__actions">
            <Link to="/orders" className="button button--quiet">← Back to Orders</Link>
            <Link to="/main" className="button button--quiet">Catalog</Link>
          </div>
        </div>
      </header>

      <div className="order-detail-container">
        <div className="order-detail-header-card">
          <div className="order-detail-title-row">
            <h1>Order #{order.id}</h1>
            {renderStatusBadge(order.status)}
          </div>
          <div className="order-detail-meta-grid">
            <div>
              <span className="meta-label">Placed on:</span>
              <p>{new Date(order.createdAt).toLocaleDateString()} {new Date(order.createdAt).toLocaleTimeString()}</p>
            </div>
            {order.updatedAt && (
              <div>
                <span className="meta-label">Last updated:</span>
                <p>{new Date(order.updatedAt).toLocaleDateString()} {new Date(order.updatedAt).toLocaleTimeString()}</p>
              </div>
            )}
            <div>
              <span className="meta-label">Total Amount:</span>
              <p className="order-total-highlight">${order.totalAmount.toFixed(2)}</p>
            </div>
          </div>
        </div>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        <div className="order-items-section">
          <h2>Items in this order</h2>
          <div className="order-items-detailed-list">
            {order.orderItems.map((item) => (
              <div key={item.id} className="order-detail-item-row">
                <div className="order-detail-image-wrapper">
                  {order.itemImages?.[item.productId] ? (
                    <img 
                      src={order.itemImages[item.productId]} 
                      alt={item.productName} 
                      className="order-detail-image" 
                    />
                  ) : (
                    <div className="order-detail-placeholder">No image</div>
                  )}
                </div>
                
                <div className="order-detail-info">
                  <Link to={`/product/${item.productId}`} className="order-product-link">
                    <h3>{item.productName}</h3>
                  </Link>
                  <p>Quantity: {item.quantity}</p>
                  <p>Price per item: ${item.price.toFixed(2)}</p>
                </div>

                <div className="order-detail-item-subtotal">
                  <span>Subtotal</span>
                  <strong>${(item.price * item.quantity).toFixed(2)}</strong>
                </div>
              </div>
            ))}
          </div>
        </div>

        {order.status !== OrderStatus.Shipped && 
         order.status !== OrderStatus.Delivered && 
         order.status !== OrderStatus.Cancelled && (
          <div className="order-actions-footer">
            <button 
              type="button" 
              className="button button--quiet text-danger"
              onClick={handleCancelOrder}
            >
              Cancel This Order
            </button>
          </div>
        )}
      </div>
    </main>
  )
}