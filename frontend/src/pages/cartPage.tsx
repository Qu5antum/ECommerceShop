import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { cartApi } from '../api/cart'
import { cartItemApi } from '../api/cart'
import { productApi } from '../api/product'
import { orderApi } from '../api/order' 
import type { CartItemResponseDto, CartItemUpdateDto } from '../types/cart'
import type { ProductResponseDto } from '../types/product'
import Brand from '../components/Brand'

interface CartItemWithProduct extends CartItemResponseDto {
  product?: ProductResponseDto | null
  imageUrl?: string | null
}

export default function CartPage() {
  const [items, setItems] = useState<CartItemWithProduct[]>([])
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const navigate = useNavigate()

  useEffect(() => {
    async function loadCartData() {
      try {
        setLoading(true)
        
        await cartApi.getCart().catch(() => null)
        const itemsData = await cartItemApi.getAllItemsFromCart().catch(() => [])

        const enrichedItems: CartItemWithProduct[] = await Promise.all(
          itemsData.map(async (item) => {
            try {
              const [product, imageUrl] = await Promise.all([
                productApi.getProductById(item.productId).catch(() => null),
                productApi.getProductImage(item.productId).catch(() => null),
              ])
              return { ...item, product, imageUrl }
            } catch {
              return { ...item, product: null, imageUrl: null }
            }
          })
        )

        setItems(enrichedItems)
      } catch (err: any) {
        setError(err.message || 'Failed to load cart')
      } finally {
        setLoading(false)
      }
    }

    loadCartData()
  }, [])

  async function handleUpdateQuantity(itemId: string, newQuantity: number) {
    if (newQuantity < 1) return

    try {
      setError('')
      const updateDto: CartItemUpdateDto = { quantity: newQuantity }
      await cartItemApi.updateCartItem(itemId, updateDto)
      
      setItems(items.map(item => 
        item.id === itemId ? { ...item, quantity: newQuantity } : item
      ))
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to update item quantity')
    }
  }

  async function handleDeleteItem(itemId: string) {
    try {
      setError('')
      await cartItemApi.deleteCartItem(itemId)
      setItems(items.filter(item => item.id !== itemId))
      setSuccessMessage('Item removed from cart.')
      setTimeout(() => setSuccessMessage(''), 3000)
    } catch (err: any) {
      setError(err.message || 'Failed to delete item')
    }
  }

  async function handleCreateOrder() {
    try {
      setError('')
      setSubmitting(true)
      
      await orderApi.createOrder()
      
      setSuccessMessage('Order created successfully!')
      setItems([]) 
      
      setTimeout(() => {
        navigate('/orders') 
      }, 2000)
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to create order')
    } finally {
      setSubmitting(false)
    }
  }

  const totalPrice = items.reduce((sum, item) => {
    const price = item.product?.price || 0
    return sum + price * item.quantity
  }, 0)

  if (loading) return <div className="loading">Loading cart...</div>

  return (
    <main className="cart-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/main" className="button button--quiet">← Back to Catalog</Link>
        </div>
      </header>

      <div className="cart-container">
        <h1>Shopping Cart</h1>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        {items.length === 0 ? (
          <div className="empty-cart">
            <p>Your cart is empty.</p>
            <Link to="/main" className="button button--dark">Start Shopping</Link>
          </div>
        ) : (
          <div className="cart-content-grid">
            <div className="cart-items-list">
              {items.map((item) => (
                <div key={item.id} className="cart-item-card">
                  <div className="cart-item-image-wrapper">
                    {item.imageUrl ? (
                      <img src={item.imageUrl} alt={item.product?.name || 'Product'} className="cart-item-image" />
                    ) : (
                      <div className="cart-item-image-placeholder">No image</div>
                    )}
                  </div>

                  <div className="cart-item-info">
                    <h3>{item.product?.name || `Product ID: ${item.productId}`}</h3>
                    <p className="cart-item-price">
                      ${item.product?.price !== undefined ? item.product.price.toFixed(2) : '0.00'}
                    </p>
                  </div>

                  <div className="cart-item-actions">
                    <div className="quantity-controls">
                      <button 
                        type="button" 
                        className="button button--quiet button--small"
                        onClick={() => handleUpdateQuantity(item.id, item.quantity - 1)}
                      >
                        -
                      </button>
                      <span>{item.quantity}</span>
                      <button 
                        type="button" 
                        className="button button--quiet button--small"
                        onClick={() => handleUpdateQuantity(item.id, item.quantity + 1)}
                      >
                        +
                      </button>
                    </div>

                    <button 
                      type="button" 
                      className="button button--quiet button--small text-danger"
                      onClick={() => handleDeleteItem(item.id)}
                    >
                      Remove
                    </button>
                  </div>
                </div>
              ))}
            </div>

            <div className="cart-summary-card">
              <h2>Order Summary</h2>
              <div className="summary-row">
                <span>Total Items:</span>
                <span>{items.reduce((acc, item) => acc + item.quantity, 0)}</span>
              </div>
              <div className="summary-row total">
                <span>Total Price:</span>
                <span>${totalPrice.toFixed(2)}</span>
              </div>
              
              <button 
                type="button" 
                className="button button--dark button--large w-100"
                onClick={handleCreateOrder}
                disabled={submitting}
              >
                {submitting ? 'Creating Order...' : 'Create Order'}
              </button>
            </div>
          </div>
        )}
      </div>
    </main>
  )
}