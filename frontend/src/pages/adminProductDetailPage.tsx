import { useEffect, useState, type FormEvent } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { productApi } from '../api/product'
import { userApi } from '../api/user'
import type { ProductResponseDto } from '../types/product'
import type { CreateNotificationDto } from '../types/product'
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminProductDetailPage.css'

export default function AdminProductDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [product, setProduct] = useState<ProductResponseDto | null>(null)
  const [currentUser, setCurrentUser] = useState<UserResponseDto | null>(null)
  const [productImageUrl, setProductImageUrl] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)
  const [notificationTitle, setNotificationTitle] = useState('Product Deleted')
  const [notificationMessage, setNotificationMessage] = useState('')

  useEffect(() => {
    async function initProductDetail() {
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

        const productData = await productApi.getProductById(id)
        setProduct(productData)

        try {
          const imgUrl = await productApi.getProductImage(id)
          setProductImageUrl(imgUrl)
        } catch {
        }
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load product details')
      } finally {
        setLoading(false)
      }
    }

    initProductDetail()
  }, [id])

  async function handleDeleteProductSubmit(e: FormEvent) {
    e.preventDefault()
    if (!id || !product) return

    try {
      setSubmitting(true)
      setError('')

      const notificationDto: CreateNotificationDto = {
        title: notificationTitle.trim() || 'Product Deleted',
        message: notificationMessage.trim() || 'Your product was removed by administration.'
      }

      await productApi.deleteProductAdmin(id, notificationDto)
      navigate('/admin/products')
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to delete product')
      setSubmitting(false)
      setIsDeleteModalOpen(false)
    }
  }

  if (loading) return <div className="loading">Loading product details...</div>

  if (error && error.includes('Access Denied')) {
    return (
      <main className="admin-product-detail-page">
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
            <Link to="/main" className="button button--dark" style={{ marginTop: '16px' }}>
              Return to Catalog
            </Link>
          </div>
        </div>
      </main>
    )
  }

  return (
    <main className="admin-product-detail-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/admin/products" className="button button--quiet">← Back to Products List</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="detail-header-row">
          <div>
            <h1>Product Management: {product?.name}</h1>
            <span className="admin-role-indicator" style={{ display: 'inline-block', marginTop: '4px' }}>
              Logged in as: <strong>{currentUser?.userName}</strong>
            </span>
          </div>
          <button 
            className="button button--danger" 
            onClick={() => setIsDeleteModalOpen(true)}
            disabled={submitting}
          >
            Delete Product (Admin)
          </button>
        </div>

        {error && <p className="form-message form-message--error">{error}</p>}

        {product && (
          <div className="product-detail-grid">
            <div className="detail-card product-image-preview-card">
              <h2>Product Image</h2>
              <div className="large-image-box">
                {productImageUrl ? (
                  <img src={productImageUrl} alt={product.name} />
                ) : (
                  <span className="no-image-placeholder">No Image Available</span>
                )}
              </div>
              <div className="stock-status-box" style={{ marginTop: '16px', textAlign: 'center' }}>
                <span className={`status-badge ${product.stock > 0 ? 'status-approved' : 'status-suspended'}`}>
                  {product.stock > 0 ? `In Stock (${product.stock} units)` : 'Out of Stock'}
                </span>
              </div>
            </div>

            <div className="detail-card">
              <h2>Product Specifications</h2>
              <div className="detail-info-list">
                <div className="detail-row">
                  <span className="label">Product ID:</span>
                  <span className="value code-text">{product.id}</span>
                </div>
                <div className="detail-row">
                  <span className="label">Name:</span>
                  <span className="value font-weight-bold">{product.name}</span>
                </div>
                <div className="detail-row">
                  <span className="label">SKU:</span>
                  <span className="value code-text">{product.sku}</span>
                </div>
                <div className="detail-row">
                  <span className="label">Price:</span>
                  <span className="value font-weight-bold" style={{ fontSize: '16px', color: '#059669' }}>
                    ${product.price.toFixed(2)}
                  </span>
                </div>
                <div className="detail-row">
                  <span className="label">Stock Quantity:</span>
                  <span className="value">{product.stock}</span>
                </div>
                <div className="detail-row">
                  <span className="label">Category ID:</span>
                  <span className="value code-text">{product.categoryId}</span>
                </div>
                <div className="detail-row">
                  <span className="label">Seller Profile ID:</span>
                  <span className="value code-text">{product.sellerProfileId}</span>
                </div>
                <div className="detail-row">
                  <span className="label">Created At:</span>
                  <span className="value">{new Date(product.createdAt).toLocaleString()}</span>
                </div>
                {product.updatedAt && (
                  <div className="detail-row">
                    <span className="label">Last Updated:</span>
                    <span className="value">{new Date(product.updatedAt).toLocaleString()}</span>
                  </div>
                )}
              </div>

              <div className="description-box" style={{ marginTop: '20px', borderTop: '1px solid #f4f4f5', paddingTop: '16px' }}>
                <span className="label" style={{ display: 'block', marginBottom: '6px', fontWeight: 500, color: '#71717a' }}>
                  Description:
                </span>
                <p style={{ margin: 0, fontSize: '14px', color: '#18181b', lineHeight: '1.5' }}>
                  {product.description || 'No description provided.'}
                </p>
              </div>
            </div>
          </div>
        )}

        {isDeleteModalOpen && (
          <div className="modal-overlay">
            <div className="modal-content">
              <h2>Delete Product & Notify Seller</h2>
              <p style={{ fontSize: '13px', color: '#71717a', marginBottom: '16px' }}>
                Provide a reason for deleting this product. This will be sent as a notification to the seller.
              </p>

              <form onSubmit={handleDeleteProductSubmit} className="product-form">
                <label className="form-field">
                  <span>Notification Title *</span>
                  <input 
                    type="text" 
                    value={notificationTitle} 
                    onChange={(e) => setNotificationTitle(e.target.value)} 
                    required 
                  />
                </label>

                <label className="form-field">
                  <span>Reason / Message *</span>
                  <textarea 
                    value={notificationMessage} 
                    onChange={(e) => setNotificationMessage(e.target.value)} 
                    placeholder="Explain why this product is being removed (e.g. violates policy)..."
                    rows={4}
                    required 
                    style={{ width: '100%', padding: '10px', borderRadius: '8px', border: '1px solid #e4e4e7', fontFamily: 'inherit' }}
                  />
                </label>

                <div className="modal-actions" style={{ display: 'flex', gap: '12px', marginTop: '16px' }}>
                  <button type="submit" className="button button--danger" disabled={submitting}>
                    {submitting ? 'Deleting...' : 'Confirm Deletion'}
                  </button>
                  <button 
                    type="button" 
                    className="button button--quiet" 
                    onClick={() => setIsDeleteModalOpen(false)}
                    disabled={submitting}
                  >
                    Cancel
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