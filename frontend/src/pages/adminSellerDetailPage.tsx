import { useEffect, useState, type FormEvent } from 'react'
import { useParams, Link } from 'react-router-dom'
import { sellerProfileApi } from '../api/seller'
import { userApi } from '../api/user'
import type { SellerProfileResponseDto, SellerStatus } from '../types/seller'
import { SellerStatus as SStatus } from '../types/seller'
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminSellerDetailPage.css'

export default function AdminSellerDetailPage() {
  const { id } = useParams<{ id: string }>()

  const [seller, setSeller] = useState<SellerProfileResponseDto | null>(null)
  const [currentUser, setCurrentUser] = useState<UserResponseDto | null>(null)
  const [sellerImageUrl, setSellerImageUrl] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const [selectedStatus, setSelectedStatus] = useState<SellerStatus>(SStatus.Pending)

  useEffect(() => {
    async function initSellerDetail() {
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

        const sellerData = await sellerProfileApi.getUserSellerProfile(id)
        setSeller(sellerData)
        setSelectedStatus(sellerData.status)

        try {
          const imgUrl = await sellerProfileApi.getSellerProfileImage(id)
          setSellerImageUrl(imgUrl)
        } catch {
        }
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load seller details')
      } finally {
        setLoading(false)
      }
    }

    initSellerDetail()
  }, [id])

  async function handleStatusUpdate(e: FormEvent) {
    e.preventDefault()
    if (!id || !seller) return

    try {
      setSubmitting(true)
      setError('')
      setSuccessMessage('')

      const statusNum = Number(selectedStatus) as SellerStatus
      await sellerProfileApi.updateStatusOfSellerProfile(id, statusNum)

      setSeller({
        ...seller,
        status: statusNum,
        updatedAt: new Date()
      })

      setSuccessMessage('Seller status successfully updated!')
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to update seller status')
    } finally {
      setSubmitting(false)
    }
  }

  function getStatusLabel(status: SellerStatus): { label: string; className: string } {
    switch (status) {
      case SStatus.Pending:
        return { label: 'Pending', className: 'status-pending' }
      case SStatus.Approved:
        return { label: 'Approved', className: 'status-approved' }
      case SStatus.Rejected:
        return { label: 'Rejected', className: 'status-suspended' }
      case SStatus.Suspended:
        return { label: 'Suspended', className: 'status-suspended' }
      default:
        return { label: 'Unknown', className: '' }
    }
  }

  if (loading) return <div className="loading">Loading seller profile details...</div>

  if (error && error.includes('Access Denied')) {
    return (
      <main className="admin-seller-detail-page">
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
    <main className="admin-seller-detail-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/admin/sellers" className="button button--quiet">← Back to Sellers List</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="detail-header-row">
          <div>
            <h1>Seller Management: {seller?.storeName}</h1>
            <span className="seller-id-subtitle">ID: {seller?.id}</span>
          </div>
          <span className="admin-role-indicator">
            Logged in as: <strong>{currentUser?.userName}</strong>
          </span>
        </div>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        {seller && (
          <div className="seller-detail-grid">
            <div className="detail-card seller-image-preview-card">
              <h2>Store Avatar / Image</h2>
              <div className="large-image-box">
                {sellerImageUrl ? (
                  <img src={sellerImageUrl} alt={seller.storeName} />
                ) : (
                  <span className="no-image-placeholder">No Store Image</span>
                )}
              </div>
              <div className="status-box-center">
                <span className={`status-badge ${getStatusLabel(seller.status).className}`}>
                  {getStatusLabel(seller.status).label}
                </span>
              </div>
            </div>

            <div className="detail-card">
              <h2>Seller Specifications</h2>
              <div className="detail-info-list">
                <div className="detail-row">
                  <span className="label">Store Name:</span>
                  <span className="value font-weight-bold">{seller.storeName}</span>
                </div>
                <div className="detail-row">
                  <span className="label">User ID:</span>
                  <span className="value code-text">{seller.userId}</span>
                </div>
                <div className="detail-row">
                  <span className="label">Seller Profile ID:</span>
                  <span className="value code-text">{seller.id}</span>
                </div>
                <div className="detail-row">
                  <span className="label">Created At:</span>
                  <span className="value">{new Date(seller.createdAt).toLocaleString()}</span>
                </div>
                {seller.updatedAt && (
                  <div className="detail-row">
                    <span className="label">Last Updated:</span>
                    <span className="value">{new Date(seller.updatedAt).toLocaleString()}</span>
                  </div>
                )}
              </div>

              <div className="description-box">
                <span className="label desc-label">Store Description:</span>
                <p className="desc-text">{seller.description || 'No description provided.'}</p>
              </div>

              <div className="status-update-section">
                <h3>Update Seller Status</h3>
                <form onSubmit={handleStatusUpdate} className="status-form">
                  <select 
                    value={selectedStatus} 
                    onChange={(e) => setSelectedStatus(Number(e.target.value) as SellerStatus)}
                    className="status-select"
                  >
                    <option value={SStatus.Pending}>Pending</option>
                    <option value={SStatus.Approved}>Approved</option>
                    <option value={SStatus.Rejected}>Rejected</option>
                    <option value={SStatus.Suspended}>Suspended</option>
                  </select>

                  <button type="submit" className="button button--dark" disabled={submitting}>
                    {submitting ? 'Updating...' : 'Save Status'}
                  </button>
                </form>
              </div>
            </div>
          </div>
        )}
      </div>
    </main>
  )
}