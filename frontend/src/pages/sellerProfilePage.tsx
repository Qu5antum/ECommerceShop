import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { sellerProfileApi } from '../api/seller'
import { orderApi } from '../api/order'
import type { 
  SellerProfileResponseDto, 
  SellerProfileCreateDto, 
  SellerProfileUpdateDto 
} from '../types/seller'
import type { OrderResponseWithOutItemsDto } from '../types/order'
import { SellerStatus } from '../types/seller'
import { OrderStatus } from '../types/order' 
import Brand from '../components/Brand'

export default function SellerProfilePage() {
  const [profile, setProfile] = useState<SellerProfileResponseDto | null>(null)
  const [orders, setOrders] = useState<OrderResponseWithOutItemsDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  const [storeName, setStoreName] = useState('')
  const [description, setDescription] = useState('')

  const [editStoreName, setEditStoreName] = useState('')
  const [editDescription, setEditDescription] = useState('')
  const [isEditing, setIsEditing] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    async function fetchSellerData() {
      try {
        setLoading(true)
        
        const [profileData, ordersData] = await Promise.all([
          sellerProfileApi.getCurrentUserSellerProfile().catch((err) => {
            if (err.response?.status !== 404) throw err
            return null
          }),
          orderApi.getOrdersOfSeller().catch(() => [])
        ])

        if (profileData) {
          setProfile(profileData)
          setEditStoreName(profileData.storeName)
          setEditDescription(profileData.description || '')
        }
        
        setOrders(ordersData)
      } catch (err: any) {
        setError(err.message || 'Failed to load seller profile data')
      } finally {
        setLoading(false)
      }
    }

    fetchSellerData()
  }, [])

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    if (!storeName.trim()) return

    try {
      setSubmitting(true)
      setError('')
      const createDto: SellerProfileCreateDto = {
        storeName,
        description: description.trim() ? description : undefined,
      }
      const newProfile = await sellerProfileApi.createSellerProfile(createDto)
      setProfile(newProfile)
      setSuccessMessage('Seller profile successfully created!')
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to create profile')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleUpdate(e: FormEvent) {
    e.preventDefault()
    if (!profile) return

    try {
      setSubmitting(true)
      setError('')
      const updateDto: SellerProfileUpdateDto = {
        storeName: editStoreName,
        description: editDescription,
      }
      
      await sellerProfileApi.updateSellerProfile(profile.id, updateDto)
      
      setProfile({
        ...profile,
        storeName: editStoreName,
        description: editDescription,
        updatedAt: new Date(),
      })
      
      setIsEditing(false)
      setSuccessMessage('Profile successfully updated!')
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to update profile')
    } finally {
      setSubmitting(false)
    }
  }

  function renderSellerStatus(status: SellerStatus) {
    switch (status) {
      case SellerStatus.Pending:
        return <span className="status-badge status-pending">Pending Review</span>
      case SellerStatus.Approved:
        return <span className="status-badge status-approved">Approved</span>
      case SellerStatus.Rejected:
        return <span className="status-badge status-rejected">Rejected</span>
      case SellerStatus.Suspended:
        return <span className="status-badge status-suspended">Suspended</span>
      default:
        return <span>Unknown</span>
    }
  }

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

  if (loading) return <div className="loading">Loading profile...</div>

  return (
    <main className="seller-profile-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/main" className="button button--quiet">← Back to Catalog</Link>
        </div>
      </header>

      <div className="seller-container">
        <h1>Seller Profile</h1>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        {!profile ? (
          <div className="profile-card">
            <h2>Create Your Store</h2>
            <p className="subtitle">You don't have a seller profile yet. Fill out the form below to start selling.</p>
            
            <form onSubmit={handleCreate} className="profile-form">
              <label className="form-field">
                <span>Store Name *</span>
                <input 
                  type="text" 
                  placeholder="My Awesome Store" 
                  value={storeName}
                  onChange={(e) => setStoreName(e.target.value)}
                  required
                />
              </label>

              <label className="form-field">
                <span>Description</span>
                <textarea 
                  rows={4} 
                  placeholder="Tell customers about your store..." 
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                />
              </label>

              <button type="submit" className="button button--dark" disabled={submitting}>
                {submitting ? 'Creating...' : 'Create Seller Profile'}
              </button>
            </form>
          </div>
        ) : (
          <>
            <div className="profile-card">
              <div className="profile-header-info">
                <div className="store-title-status">
                  <h2>{profile.storeName}</h2>
                  {renderSellerStatus(profile.status)}
                </div>
                {!isEditing && (
                  <button className="button button--quiet" onClick={() => setIsEditing(true)}>
                    Edit Profile
                  </button>
                )}
              </div>

              {!isEditing ? (
                <div className="profile-details">
                  <p className="profile-desc">
                    <strong>Description:</strong> {profile.description || 'No description provided.'}
                  </p>
                  <div className="profile-dates">
                    <span>Created: {new Date(profile.createdAt).toLocaleDateString()}</span>
                    {profile.updatedAt && <span>Updated: {new Date(profile.updatedAt).toLocaleDateString()}</span>}
                  </div>
                </div>
              ) : (
                <form onSubmit={handleUpdate} className="profile-form">
                  <label className="form-field">
                    <span>Store Name</span>
                    <input 
                      type="text" 
                      value={editStoreName}
                      onChange={(e) => setEditStoreName(e.target.value)}
                      required
                    />
                  </label>

                  <label className="form-field">
                    <span>Description</span>
                    <textarea 
                      rows={4} 
                      value={editDescription}
                      onChange={(e) => setEditDescription(e.target.value)}
                    />
                  </label>

                  <div className="form-actions">
                    <button type="submit" className="button button--dark" disabled={submitting}>
                      {submitting ? 'Saving...' : 'Save Changes'}
                    </button>
                    <button type="button" className="button button--quiet" onClick={() => setIsEditing(false)}>
                      Cancel
                    </button>
                  </div>
                </form>
              )}
            </div>

            <section className="seller-orders-section">
              <h2>Store Orders ({orders.length})</h2>

              {orders.length === 0 ? (
                <p className="no-orders">No orders received yet.</p>
              ) : (
                <div className="orders-list">
                  {orders.map((order) => (
                    <div key={order.id} className="order-card">
                      <div className="order-info">
                        <h4>Order ID: {order.id}</h4>
                        <p className="order-date">
                          Date: {new Date(order.createdAt).toLocaleDateString()}
                        </p>
                      </div>
                      <div className="order-meta">
                        <span className="order-amount">${order.totalAmount}</span>
                        {renderOrderStatus(order.status)}
                        <Link to={`/seller/order/${order.id}/detail`} className="button button--quiet order-details-link">
                          View Details →
                        </Link>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </section>
          </>
        )}
      </div>
    </main>
  )
}