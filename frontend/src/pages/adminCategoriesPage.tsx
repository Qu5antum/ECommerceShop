import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { categoryApi } from '../api/category'
import { userApi } from '../api/user'
import type { CategoryResponseDto, CategoryCreateDto } from '../types/category'
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminCategoriesPage.css'

export default function AdminCategoriesPage() {
  const navigate = useNavigate()
  const [currentUser, setCurrentUser] = useState<UserResponseDto | null>(null)
  const [categories, setCategories] = useState<CategoryResponseDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [title, setTitle] = useState('')
  const [slug, setSlug] = useState('')

  useEffect(() => {
    async function initAdminCategories() {
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

        await fetchCategories()
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load categories')
        setLoading(false)
      }
    }

    initAdminCategories()
  }, [])

  async function fetchCategories() {
    try {
      const categoriesData = await categoryApi.getAllCategories()
      setCategories(categoriesData)
    } catch (err: any) {
      setError(err.message || 'Failed to fetch categories list')
    } finally {
      setLoading(false)
    }
  }

  function handleTitleChange(value: string) {
    setTitle(value)
    const generatedSlug = value
      .toLowerCase()
      .trim()
      .replace(/[^\w\s-]/g, '')
      .replace(/[\s_-]+/g, '-')
      .replace(/^-+|-+$/g, '')
    setSlug(generatedSlug)
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault()
    try {
      setSubmitting(true)
      setError('')
      setSuccessMessage('')

      const createDto: CategoryCreateDto = {
        title: title.trim(),
        slug: slug.trim()
      }

      await categoryApi.createCategory(createDto)
      setSuccessMessage('Category successfully created!')
      setIsCreateOpen(false)
      setTitle('')
      setSlug('')
      await fetchCategories()
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to create category')
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <div className="loading">Checking permissions and loading categories...</div>

  if (error && error.includes('Access Denied')) {
    return (
      <main className="admin-categories-page">
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
    <main className="admin-categories-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/user/profile" className="button button--quiet">← Back to Admin Panel</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="admin-header-row">
          <h1>Categories Management</h1>
          <div className="admin-header-actions" style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
            <button className="button button--dark" onClick={() => setIsCreateOpen(true)}>
              + Add New Category
            </button>
            <span className="admin-role-indicator">
              Logged in as: <strong>{currentUser?.userName}</strong>
            </span>
          </div>
        </div>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        <div className="categories-table-card">
          <h2>All Categories ({categories.length})</h2>

          {categories.length === 0 ? (
            <p className="no-categories">No categories found.</p>
          ) : (
            <div className="table-responsive">
              <table className="admin-categories-table">
                <thead>
                  <tr>
                    <th>Title</th>
                    <th>Slug</th>
                    <th>Created At</th>
                    <th>Last Updated</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {categories.map((cat) => (
                    <tr 
                      key={cat.id} 
                      className="category-row"
                      onClick={() => navigate(`/admin/categories/${cat.id}`)}
                      style={{ cursor: 'pointer' }}
                    >
                      <td className="font-weight-bold">{cat.title}</td>
                      <td><code>{cat.slug}</code></td>
                      <td>{new Date(cat.createdAt).toLocaleDateString()}</td>
                      <td>{cat.updatedAt ? new Date(cat.updatedAt).toLocaleDateString() : '—'}</td>
                      <td>
                        <Link 
                          to={`/admin/categories/${cat.id}`} 
                          className="button button--quiet"
                          onClick={(e) => e.stopPropagation()}
                        >
                          Details →
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {isCreateOpen && (
          <div className="modal-overlay">
            <div className="modal-content">
              <h2>Create New Category</h2>
              <form onSubmit={handleCreate} className="product-form">
                <label className="form-field">
                  <span>Category Title *</span>
                  <input 
                    type="text" 
                    value={title} 
                    onChange={(e) => handleTitleChange(e.target.value)} 
                    placeholder="e.g. Electronics"
                    required 
                  />
                </label>

                <label className="form-field">
                  <span>Slug *</span>
                  <input 
                    type="text" 
                    value={slug} 
                    onChange={(e) => setSlug(e.target.value)} 
                    placeholder="e.g. electronics"
                    required 
                  />
                </label>

                <div className="modal-actions">
                  <button type="submit" className="button button--dark" disabled={submitting}>
                    {submitting ? 'Creating...' : 'Create Category'}
                  </button>
                  <button 
                    type="button" 
                    className="button button--quiet" 
                    onClick={() => {
                      setIsCreateOpen(false)
                      setTitle('')
                      setSlug('')
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