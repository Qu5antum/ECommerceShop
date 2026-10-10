import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { userApi } from '../api/user'
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminUserPage.css'

export default function AdminUsersPage() {
  const [currentUser, setCurrentUser] = useState<UserResponseDto | null>(null)
  const [users, setUsers] = useState<UserResponseDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    async function initAdminPage() {
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

        const usersData = await userApi.getAllUsersNotAdmin()
        setUsers(usersData)
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load users data')
      } finally {
        setLoading(false)
      }
    }

    initAdminPage()
  }, [])

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

  if (loading) return <div className="loading">Checking permissions and loading users...</div>

  if (error && error.includes('Access Denied')) {
    return (
      <main className="admin-users-page">
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
    <main className="admin-users-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/user/profile" className="button button--quiet">← Back to Profile</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="admin-header-row">
          <h1>User Management Panel</h1>
          <span className="admin-role-indicator">
            Logged in as: <strong>{currentUser?.userName}</strong>
          </span>
        </div>

        {error && <p className="form-message form-message--error">{error}</p>}

        <div className="users-table-card">
          <h2>Registered Users ({users.length})</h2>

          {users.length === 0 ? (
            <p className="no-users">No users found.</p>
          ) : (
            <div className="table-responsive">
              <table className="admin-users-table">
                <thead>
                  <tr>
                    <th>Username</th>
                    <th>Email</th>
                    <th>Roles</th>
                    <th>Status</th>
                    <th>Created At</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((u) => (
                    <tr key={u.id}>
                      <td className="font-weight-bold">
                        <Link to={`/admin/users/${u.id}`} className="user-detail-link">
                          {u.userName}
                        </Link>
                      </td>
                      <td>{u.email}</td>
                      <td>{renderRoles(u.roles)}</td>
                      <td>
                        <span className={`status-badge ${u.isActive ? 'status-approved' : 'status-suspended'}`}>
                          {u.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td>{new Date(u.createdAt).toLocaleDateString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </main>
  )
}