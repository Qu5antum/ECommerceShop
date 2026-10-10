import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { userApi } from '../api/user' 
import type { UserResponseDto, UserUpdateDto, UserPasswordUpdateDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/userProfilePage.css'

export default function UserProfilePage() {
  const [user, setUser] = useState<UserResponseDto | null>(null)
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
    async function fetchUserProfile() {
      try {
        setLoading(true)
        setError('')
        const data = await userApi.getCurrentUserProfile()
        setUser(data)
        setUserName(data.userName)
        setEmail(data.email)
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load user profile')
      } finally {
        setLoading(false)
      }
    }

    fetchUserProfile()
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

  const hasManagementAccess = user ? (user.roles & (UserRole.Admin | UserRole.Manager | UserRole.Moderator)) !== 0 : false

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
                  <div className="admin-dashboard-link-box" style={{ marginTop: '12px' }}>
                    <Link to="/admin/users" className="button button--dark" style={{ background: '#7c3aed' }}>
                      Manage Users (Admin Panel) →
                    </Link>
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