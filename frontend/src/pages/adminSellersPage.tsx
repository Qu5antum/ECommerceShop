import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { sellerProfileApi } from '../api/seller' 
import { userApi } from '../api/user'
import type { SellerProfileResponseDto, SellerStatus } from '../types/seller'
import { SellerStatus as SStatus } from '../types/seller'
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminSellersPage.css'

export default function AdminSellersPage() {
  const navigate = useNavigate()
  const [currentUser, setCurrentUser] = useState<UserResponseDto | null>(null)
  const [sellers, setSellers] = useState<SellerProfileResponseDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [selectedStatus, setSelectedStatus] = useState<string>('all')

  useEffect(() => {
    async function initAdminSellers() {
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

        await fetchSellers()
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to initialize sellers page')
        setLoading(false)
      }
    }

    initAdminSellers()
  }, [])

  async function fetchSellers(statusFilter?: SellerStatus) {
    try {
      setLoading(true)
      setError('')

      let data: SellerProfileResponseDto[] = []
      if (statusFilter !== undefined && !isNaN(statusFilter)) {
        data = await sellerProfileApi.getSellersByStatus(statusFilter)
      } else {
        const allStatuses = [SStatus.Pending, SStatus.Approved, SStatus.Rejected, SStatus.Suspended]
        const promises = allStatuses.map((st) => sellerProfileApi.getSellersByStatus(st).catch(() => []))
        const results = await Promise.all(promises)
        const flat = results.flat()
        const uniqueMap = new Map()
        flat.forEach((item) => uniqueMap.set(item.id, item))
        data = Array.from(uniqueMap.values())
      }

      setSellers(data)
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to load sellers list')
    } finally {
      setLoading(false)
    }
  }

  function handleStatusChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const val = e.target.value
    setSelectedStatus(val)
    const statusNum = val === 'all' ? undefined : (Number(val) as SellerStatus)
    fetchSellers(statusNum)
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

  if (loading && sellers.length === 0) return <div className="loading">Loading sellers management...</div>

  if (error && error.includes('Access Denied')) {
    return (
      <main className="admin-sellers-page">
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
    <main className="admin-sellers-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/user/profile" className="button button--quiet">← Back to Admin Panel</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="admin-header-row">
          <h1>Sellers Profiles Management</h1>
          <span className="admin-role-indicator">
            Logged in as: <strong>{currentUser?.userName}</strong>
          </span>
        </div>

        <div className="filters-card">
          <div className="filter-field">
            <label>Filter by Seller Status</label>
            <select value={selectedStatus} onChange={handleStatusChange}>
              <option value="all">All Statuses</option>
              <option value={SStatus.Pending}>Pending</option>
              <option value={SStatus.Approved}>Approved</option>
              <option value={SStatus.Rejected}>Rejected</option>
              <option value={SStatus.Suspended}>Suspended</option>
            </select>
          </div>
        </div>

        {error && <p className="form-message form-message--error">{error}</p>}

        <div className="sellers-table-card">
          <h2>Sellers Profiles ({sellers.length})</h2>

          {sellers.length === 0 ? (
            <p className="no-sellers">No seller profiles found.</p>
          ) : (
            <div className="table-responsive">
              <table className="admin-sellers-table">
                <thead>
                  <tr>
                    <th>Store Name</th>
                    <th>User ID</th>
                    <th>Status</th>
                    <th>Description</th>
                    <th>Created At</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {sellers.map((seller) => {
                    const statusInfo = getStatusLabel(seller.status)
                    return (
                      <tr 
                        key={seller.id} 
                        className="seller-row-clickable"
                        onClick={() => navigate(`/admin/sellers/${seller.id}`)}
                      >
                        <td className="font-weight-bold">{seller.storeName}</td>
                        <td className="seller-user-cell">{seller.userId}</td>
                        <td>
                          <span className={`status-badge ${statusInfo.className}`}>
                            {statusInfo.label}
                          </span>
                        </td>
                        <td className="seller-desc-cell">
                          {seller.description ? seller.description : '—'}
                        </td>
                        <td className="seller-date-cell">
                          {new Date(seller.createdAt).toLocaleDateString()}
                        </td>
                        <td>
                          <Link 
                            to={`/admin/sellers/${seller.id}`} 
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