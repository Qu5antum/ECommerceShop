import { useEffect, useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { userApi } from '../api/user' 
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminUserDetailPage.css'

export default function AdminUserDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [user, setUser] = useState<UserResponseDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!id) return
    fetchUserData(id)
  }, [id])

  async function fetchUserData(userId: string) {
    try {
      setLoading(true)
      setError('')
      const data = await userApi.getUserById(userId)
      setUser(data)
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to load user details')
    } finally {
      setLoading(false)
    }
  }

  async function handleToggleActive() {
    if (!user) return
    try {
      setSubmitting(true)
      setError('')
      const newActiveStatus = !user.isActive
      await userApi.activateOrDeactivateUser(user.id, newActiveStatus)
      setUser({ ...user, isActive: newActiveStatus, updatedAt: new Date() })
      setSuccessMessage(`User status successfully updated to ${newActiveStatus ? 'Active' : 'Inactive'}`)
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to change user status')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleAddRole(roleValue: number) {
    if (!user) return
    try {
      setSubmitting(true)
      setError('')
      await userApi.addRoleToUser(user.id, roleValue)
      
      await fetchUserData(user.id)
      setSuccessMessage('Role successfully added!')
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to add role')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleRemoveRole(roleValue: number) {
    if (!user) return
    try {
      setSubmitting(true)
      setError('')
      await userApi.removeRoleFromUser(user.id, roleValue)
      
      await fetchUserData(user.id)
      setSuccessMessage('Role successfully removed!')
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to remove role')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeleteUser() {
    if (!user) return
    if (!window.confirm(`Are you sure you want to delete user "${user.userName}"? This action cannot be undone.`)) return

    try {
      setSubmitting(true)
      setError('')
      await userApi.deleteUser(user.id)
      navigate('/admin/users')
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to delete user')
      setSubmitting(false)
    }
  }

  if (loading) return <div className="loading">Loading user details...</div>
  if (error && !user) return <div className="error-container"><h2>Error</h2><p>{error}</p><Link to="/admin/users">Back to Users</Link></div>
  if (!user) return <div className="loading">User not found</div>

  const rolesList = [
    { name: 'User', value: UserRole.DefaultUser },
    { name: 'Manager', value: UserRole.Manager },
    { name: 'Seller', value: UserRole.Seller },
    { name: 'Moderator', value: UserRole.Moderator },
    { name: 'Admin', value: UserRole.Admin },
  ]

  return (
    <main className="admin-user-detail-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/admin/users" className="button button--quiet">← Back to Users List</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="detail-header-row">
          <div>
            <h1>User Management: {user.userName}</h1>
            <span className="user-id-subtitle">ID: {user.id}</span>
          </div>
          <button 
            className="button button--danger" 
            onClick={handleDeleteUser} 
            disabled={submitting}
          >
            Delete User
          </button>
        </div>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        <div className="user-detail-grid">
          <div className="detail-card">
            <h2>Account Details</h2>
            <div className="detail-info-list">
              <div className="detail-row">
                <span className="label">Username:</span>
                <span className="value font-weight-bold">{user.userName}</span>
              </div>
              <div className="detail-row">
                <span className="label">Email:</span>
                <span className="value">{user.email}</span>
              </div>
              <div className="detail-row">
                <span className="label">Status:</span>
                <span className={`status-badge ${user.isActive ? 'status-approved' : 'status-suspended'}`}>
                  {user.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
              <div className="detail-row">
                <span className="label">Created At:</span>
                <span className="value">{new Date(user.createdAt).toLocaleString()}</span>
              </div>
              {user.updatedAt && (
                <div className="detail-row">
                  <span className="label">Last Updated:</span>
                  <span className="value">{new Date(user.updatedAt).toLocaleString()}</span>
                </div>
              )}
            </div>

            <div className="action-row" style={{ marginTop: '24px' }}>
              <button 
                className={`button ${user.isActive ? 'button--danger' : 'button--dark'}`}
                onClick={handleToggleActive}
                disabled={submitting}
              >
                {user.isActive ? 'Deactivate Account' : 'Activate Account'}
              </button>
            </div>
          </div>

          <div className="detail-card">
            <h2>Role Management</h2>
            <p className="subtitle" style={{ fontSize: '13px', color: '#71717a', marginBottom: '16px' }}>
              Click to assign or revoke individual roles for this user.
            </p>

            <div className="roles-management-list">
              {rolesList.map((r) => {
                const hasRole = (user.roles & r.value) === r.value
                return (
                  <div key={r.name} className="role-manage-item">
                    <span className="role-name">{r.name}</span>
                    {hasRole ? (
                      <button 
                        className="button button--quiet role-action-btn remove"
                        onClick={() => handleRemoveRole(r.value)}
                        disabled={submitting}
                      >
                        Remove Role
                      </button>
                    ) : (
                      <button 
                        className="button button--dark role-action-btn add"
                        onClick={() => handleAddRole(r.value)}
                        disabled={submitting}
                      >
                        Add Role
                      </button>
                    )}
                  </div>
                )
              })}
            </div>
          </div>
        </div>
      </div>
    </main>
  )
}