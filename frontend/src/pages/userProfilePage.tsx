import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { userApi } from '../api/user'
import { analyticApi } from '../api/analytic'
import type { UserResponseDto, UserUpdateDto, UserPasswordUpdateDto } from '../types/user'
import type { GeneralAnalyticsResponseDto } from '../types/analytic'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/userProfilePage.css'

export default function UserProfilePage() {
  const [user, setUser] = useState<UserResponseDto | null>(null)
  const [analytics, setAnalytics] = useState<GeneralAnalyticsResponseDto>({
    users: 0,
    sellers: 0,
    products: 0,
    orders: 0,
    pendingOrders: 0,
    pendingPayments: 0,
    revenue: 0,
    reviews: 0
  })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const [userName, setUserName] = useState('')
  const [email, setEmail] = useState('')
  const [isEditing, setIsEditing] = useState(false)

  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false)
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [passwordSubmitting, setPasswordSubmitting] = useState(false)
  const [passwordError, setPasswordError] = useState('')
  const [passwordSuccess, setPasswordSuccess] = useState('')

  useEffect(() => {
    async function fetchUserData() {
      try {
        setLoading(true)
        setError('')

        const profileData = await userApi.getCurrentUserProfile()
        setUser(profileData)
        setUserName(profileData.userName)
        setEmail(profileData.email)

        const allowedRoles = UserRole.Admin | UserRole.Manager | UserRole.Moderator
        if ((profileData.roles & allowedRoles) !== 0) {
          try {
            const rawAnalytics = await analyticApi.getGeneralAnalyticsAdmin()
            setAnalytics(rawAnalytics)
          } catch (err: any) {
            console.error('Failed to load summary analytics:', err)
          }
        }
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load user profile')
      } finally {
        setLoading(false)
      }
    }

    fetchUserData()
  }, [])

  async function handleUpdate(e: FormEvent) {
    e.preventDefault()
    try {
      setSubmitting(true)
      setError('')
      setSuccessMessage('')

      const updateDto: UserUpdateDto = {
        userName: userName.trim() || undefined,
        email: email.trim() || undefined,
      }

      await userApi.updateProfile(updateDto)

      setUser((prev) => prev ? {
        ...prev,
        userName: userName,
        email: email,
        updatedAt: new Date()
      } : null)

      setSuccessMessage('Profile successfully updated!')
      setIsEditing(false)
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to update profile')
    } finally {
      setSubmitting(false)
    }
  }

  async function handlePasswordChange(e: FormEvent) {
    e.preventDefault()
    if (newPassword !== confirmPassword) {
      setPasswordError('New passwords do not match')
      return
    }

    try {
      setPasswordSubmitting(true)
      setPasswordError('')
      setPasswordSuccess('')

      const passwordDto: UserPasswordUpdateDto = {
        password: newPassword,
        confirmPassword: confirmPassword || undefined
      }

      await userApi.changePassword(passwordDto)

      setPasswordSuccess('Password successfully updated!')
      setNewPassword('')
      setConfirmPassword('')
      setTimeout(() => {
        setIsPasswordModalOpen(false)
        setPasswordSuccess('')
      }, 1500)
    } catch (err: any) {
      setPasswordError(err.response?.data?.message || err.message || 'Failed to update password')
    } finally {
      setPasswordSubmitting(false)
    }
  }

  function renderRoles(rolesValue: UserRole) {
    const activeRoles: string[] = []
    if ((rolesValue & UserRole.DefaultUser) === UserRole.DefaultUser) activeRoles.push('User')
    if ((rolesValue & UserRole.Admin) === UserRole.Admin) activeRoles.push('Admin')
    if ((rolesValue & UserRole.Manager) === UserRole.Manager) activeRoles.push('Manager')
    if ((rolesValue & UserRole.Seller) === UserRole.Seller) activeRoles.push('Seller')
    if ((rolesValue & UserRole.Moderator) === UserRole.Moderator) activeRoles.push('Moderator')

    return (
      <div className="roles-badges">
        {activeRoles.map((role) => (
          <span key={role} className={`role-badge role-${role.toLowerCase()}`}>
            {role}
          </span>
        ))}
      </div>
    )
  }

  if (loading) return <div className="loading">Loading profile...</div>

  const allowedRoles = UserRole.Admin | UserRole.Manager | UserRole.Moderator
  const hasManagementAccess = user ? (user.roles & allowedRoles) !== 0 : false

  return (
    <main className="user-profile-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/main" className="button button--quiet">← Back to Catalog</Link>
        </div>
      </header>

      <div className="user-profile-container">
        <h1>My Profile</h1>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        {user && (
          <div className="profile-card">
            <div className="profile-header-info">
              <div className="user-main-info">
                <h2>{user.userName}</h2>
                <span className={`status-badge ${user.isActive ? 'status-approved' : 'status-suspended'}`}>
                  {user.isActive ? 'Active Account' : 'Inactive'}
                </span>
              </div>
              <div className="profile-top-actions">
                {!isEditing && (
                  <>
                    <button className="button button--quiet" onClick={() => setIsEditing(true)}>
                      Edit Profile
                    </button>
                    <button className="button button--quiet" onClick={() => setIsPasswordModalOpen(true)}>
                      Change Password
                    </button>
                  </>
                )}
              </div>
            </div>

            {!isEditing ? (
              <div className="profile-details">
                <div className="detail-item">
                  <span className="detail-label">Email:</span>
                  <span className="detail-value">{user.email}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Roles:</span>
                  <span className="detail-value">{renderRoles(user.roles)}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Member Since:</span>
                  <span className="detail-value">{new Date(user.createdAt).toLocaleDateString()}</span>
                </div>
                {user.updatedAt && (
                  <div className="detail-item">
                    <span className="detail-label">Last Updated:</span>
                    <span className="detail-value">{new Date(user.updatedAt).toLocaleDateString()}</span>
                  </div>
                )}

                {(user.roles & UserRole.Seller) === UserRole.Seller && (
                  <div className="seller-dashboard-link-box" style={{ marginTop: '20px' }}>
                    <Link to="/seller/profile" className="button button--dark">
                      Go to Seller Dashboard →
                    </Link>
                  </div>
                )}

                {hasManagementAccess && (
                  <div className="admin-management-section" style={{ marginTop: '30px', borderTop: '1px solid #e4e4e7', paddingTop: '20px' }}>
                    <h3 style={{ fontSize: '18px', fontWeight: 600, marginBottom: '16px', color: '#111' }}>
                      Management & Analytics Summary
                    </h3>

                    {/* Карточки аналитики рендерятся всегда */}
                    <div className="admin-stats-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(130px, 1fr))', gap: '12px', marginBottom: '24px' }}>
                      <div style={{ background: '#f8fafc', border: '1px solid #e2e8f0', padding: '12px', borderRadius: '8px' }}>
                        <span style={{ fontSize: '11px', color: '#64748b', textTransform: 'uppercase', display: 'block' }}>Users</span>
                        <span style={{ fontSize: '18px', fontWeight: 700, color: '#0f172a' }}>{analytics.users}</span>
                      </div>
                      <div style={{ background: '#f8fafc', border: '1px solid #e2e8f0', padding: '12px', borderRadius: '8px' }}>
                        <span style={{ fontSize: '11px', color: '#64748b', textTransform: 'uppercase', display: 'block' }}>Sellers</span>
                        <span style={{ fontSize: '18px', fontWeight: 700, color: '#0f172a' }}>{analytics.sellers}</span>
                      </div>
                      <div style={{ background: '#f8fafc', border: '1px solid #e2e8f0', padding: '12px', borderRadius: '8px' }}>
                        <span style={{ fontSize: '11px', color: '#64748b', textTransform: 'uppercase', display: 'block' }}>Products</span>
                        <span style={{ fontSize: '18px', fontWeight: 700, color: '#0f172a' }}>{analytics.products}</span>
                      </div>
                      <div style={{ background: '#f8fafc', border: '1px solid #e2e8f0', padding: '12px', borderRadius: '8px' }}>
                        <span style={{ fontSize: '11px', color: '#64748b', textTransform: 'uppercase', display: 'block' }}>Orders</span>
                        <span style={{ fontSize: '18px', fontWeight: 700, color: '#0f172a' }}>{analytics.orders}</span>
                      </div>
                      <div style={{ background: '#f8fafc', border: '1px solid #e2e8f0', padding: '12px', borderRadius: '8px' }}>
                        <span style={{ fontSize: '11px', color: '#64748b', textTransform: 'uppercase', display: 'block' }}>Pending Orders</span>
                        <span style={{ fontSize: '18px', fontWeight: 700, color: '#d97706' }}>{analytics.pendingOrders}</span>
                      </div>
                      <div style={{ background: '#f8fafc', border: '1px solid #e2e8f0', padding: '12px', borderRadius: '8px' }}>
                        <span style={{ fontSize: '11px', color: '#64748b', textTransform: 'uppercase', display: 'block' }}>Pending Payments</span>
                        <span style={{ fontSize: '18px', fontWeight: 700, color: '#d97706' }}>{analytics.pendingPayments}</span>
                      </div>
                      <div style={{ background: '#f8fafc', border: '1px solid #e2e8f0', padding: '12px', borderRadius: '8px' }}>
                        <span style={{ fontSize: '11px', color: '#64748b', textTransform: 'uppercase', display: 'block' }}>Revenue</span>
                        <span style={{ fontSize: '18px', fontWeight: 700, color: '#059669' }}>${Number(analytics.revenue).toFixed(2)}</span>
                      </div>
                      <div style={{ background: '#f8fafc', border: '1px solid #e2e8f0', padding: '12px', borderRadius: '8px' }}>
                        <span style={{ fontSize: '11px', color: '#64748b', textTransform: 'uppercase', display: 'block' }}>Reviews</span>
                        <span style={{ fontSize: '18px', fontWeight: 700, color: '#0f172a' }}>{analytics.reviews}</span>
                      </div>
                    </div>

                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '10px' }}>
                      <Link to="/admin/users" className="button button--dark" style={{ background: '#7c3aed', textAlign: 'center', justifyContent: 'center' }}>
                        Users Panel →
                      </Link>
                      <Link to="/admin/categories" className="button button--dark" style={{ background: '#7c3aed', textAlign: 'center', justifyContent: 'center' }}>
                        Categories →
                      </Link>
                      <Link to="/admin/products" className="button button--dark" style={{ background: '#7c3aed', textAlign: 'center', justifyContent: 'center' }}>
                        Products →
                      </Link>
                      <Link to="/admin/orders" className="button button--dark" style={{ background: '#7c3aed', textAlign: 'center', justifyContent: 'center' }}>
                        Orders →
                      </Link>
                      <Link to="/admin/payments" className="button button--dark" style={{ background: '#7c3aed', textAlign: 'center', justifyContent: 'center' }}>
                        Payments →
                      </Link>
                      <Link to="/admin/sellers" className="button button--dark" style={{ background: '#7c3aed', textAlign: 'center', justifyContent: 'center' }}>
                        Seller Profiles →
                      </Link>
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <form onSubmit={handleUpdate} className="profile-form">
                <label className="form-field">
                  <span>Username</span>
                  <input 
                    type="text" 
                    value={userName} 
                    onChange={(e) => setUserName(e.target.value)} 
                    required 
                  />
                </label>

                <label className="form-field">
                  <span>Email</span>
                  <input 
                    type="email" 
                    value={email} 
                    onChange={(e) => setEmail(e.target.value)} 
                    required 
                  />
                </label>

                <div className="form-actions">
                  <button type="submit" className="button button--dark" disabled={submitting}>
                    {submitting ? 'Saving...' : 'Save Changes'}
                  </button>
                  <button 
                    type="button" 
                    className="button button--quiet" 
                    onClick={() => {
                      setIsEditing(false)
                      setUserName(user.userName)
                      setEmail(user.email)
                    }}
                  >
                    Cancel
                  </button>
                </div>
              </form>
            )}
          </div>
        )}

        {isPasswordModalOpen && (
          <div className="modal-overlay">
            <div className="modal-content">
              <h2>Change Password</h2>
              
              {passwordSuccess && <p className="form-message form-message--success">{passwordSuccess}</p>}
              {passwordError && <p className="form-message form-message--error">{passwordError}</p>}

              <form onSubmit={handlePasswordChange} className="product-form">
                <label className="form-field">
                  <span>New Password *</span>
                  <input 
                    type="password" 
                    placeholder="••••••••" 
                    value={newPassword} 
                    onChange={(e) => setNewPassword(e.target.value)} 
                    required 
                  />
                </label>

                <label className="form-field">
                  <span>Confirm New Password *</span>
                  <input 
                    type="password" 
                    placeholder="••••••••" 
                    value={confirmPassword} 
                    onChange={(e) => setConfirmPassword(e.target.value)} 
                    required 
                  />
                </label>

                <div className="modal-actions">
                  <button type="submit" className="button button--dark" disabled={passwordSubmitting}>
                    {passwordSubmitting ? 'Updating...' : 'Update Password'}
                  </button>
                  <button 
                    type="button" 
                    className="button button--quiet" 
                    onClick={() => {
                      setIsPasswordModalOpen(false)
                      setNewPassword('')
                      setConfirmPassword('')
                      setPasswordError('')
                      setPasswordSuccess('')
                    }}
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