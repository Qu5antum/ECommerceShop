import { useEffect, useState, type FormEvent } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { categoryApi } from '../api/category'
import { userApi } from '../api/user'
import type { CategoryResponseDto, CategoryUpdateDto } from '../types/category'
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminCategoryDetailPage.css'

export default function AdminCategoryDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [category, setCategory] = useState<CategoryResponseDto | null>(null)
  const [currentUser, setCurrentUser] = useState<UserResponseDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [submitting, setSubmitting] = useState(false)

  // Режим редактирования и поля формы
  const [isEditing, setIsEditing] = useState(false)
  const [title, setTitle] = useState('')
  const [slug, setSlug] = useState('')

  useEffect(() => {
    async function initCategoryDetail() {
      if (!id) return
      try {
        setLoading(true)
        setError('')

        // 1. Проверяем права пользователя
        const profile = await userApi.getCurrentUserProfile()
        setCurrentUser(profile)

        const allowedRoles = UserRole.Admin | UserRole.Manager | UserRole.Moderator
        if (!profile || (profile.roles & allowedRoles) === 0) {
          setError('Access Denied: You do not have permission to view this page.')
          setLoading(false)
          return
        }

        // 2. Получаем данные категории по ID
        const categoryData = await categoryApi.getCategoryById(id)
        setCategory(categoryData)
        setTitle(categoryData.title)
        setSlug(categoryData.slug)
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load category details')
      } finally {
        setLoading(false)
      }
    }

    initCategoryDetail()
  }, [id])

  async function handleUpdate(e: FormEvent) {
    e.preventDefault()
    if (!id || !category) return

    try {
      setSubmitting(true)
      setError('')
      setSuccessMessage('')

      const updateDto: CategoryUpdateDto = {
        title: title.trim() || undefined,
        slug: slug.trim() || undefined,
      }

      await categoryApi.updateCategory(id, updateDto)

      // Обновляем локальное состояние
      setCategory({
        ...category,
        title: title.trim(),
        slug: slug.trim(),
        updatedAt: new Date(),
      })

      setSuccessMessage('Category successfully updated!')
      setIsEditing(false)
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to update category')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeleteCategory() {
    if (!id || !category) return
    if (!window.confirm(`Are you sure you want to delete category "${category.title}"?`)) return

    try {
      setSubmitting(true)
      setError('')
      await categoryApi.deleteCategory(id)
      navigate('/admin/categories')
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to delete category')
      setSubmitting(false)
    }
  }

  if (loading) return <div className="loading">Loading category details...</div>

  if (error && error.includes('Access Denied')) {
    return (
      <main className="admin-category-detail-page">
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
    <main className="admin-category-detail-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/admin/categories" className="button button--quiet">← Back to Categories List</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="detail-header-row">
          <div>
            <h1>Category Management</h1>
            <span className="admin-role-indicator" style={{ display: 'inline-block', marginTop: '4px' }}>
              Logged in as: <strong>{currentUser?.userName}</strong>
            </span>
          </div>
          <button 
            className="button button--danger" 
            onClick={handleDeleteCategory}
            disabled={submitting}
          >
            Delete Category
          </button>
        </div>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        {category && (
          <div className="detail-card">
            <div className="profile-header-info" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <h2>Category ID: <span style={{ fontFamily: 'monospace', fontSize: '14px', color: '#71717a' }}>{category.id}</span></h2>
              {!isEditing && (
                <button className="button button--quiet" onClick={() => setIsEditing(true)}>
                  Edit Category
                </button>
              )}
            </div>

            {!isEditing ? (
              <div className="profile-details">
                <div className="detail-item">
                  <span className="detail-label">Title:</span>
                  <span className="detail-value font-weight-bold">{category.title}</span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Slug:</span>
                  <span className="detail-value"><code>{category.slug}</code></span>
                </div>
                <div className="detail-item">
                  <span className="detail-label">Created At:</span>
                  <span className="detail-value">{new Date(category.createdAt).toLocaleString()}</span>
                </div>
                {category.updatedAt && (
                  <div className="detail-item">
                    <span className="detail-label">Last Updated:</span>
                    <span className="detail-value">{new Date(category.updatedAt).toLocaleString()}</span>
                  </div>
                )}
              </div>
            ) : (
              <form onSubmit={handleUpdate} className="product-form">
                <label className="form-field">
                  <span>Category Title *</span>
                  <input 
                    type="text" 
                    value={title} 
                    onChange={(e) => setTitle(e.target.value)} 
                    required 
                  />
                </label>

                <label className="form-field">
                  <span>Slug *</span>
                  <input 
                    type="text" 
                    value={slug} 
                    onChange={(e) => setSlug(e.target.value)} 
                    required 
                  />
                </label>

                <div className="form-actions" style={{ display: 'flex', gap: '12px', marginTop: '16px' }}>
                  <button type="submit" className="button button--dark" disabled={submitting}>
                    {submitting ? 'Saving...' : 'Save Changes'}
                  </button>
                  <button 
                    type="button" 
                    className="button button--quiet" 
                    onClick={() => {
                      setIsEditing(false)
                      setTitle(category.title)
                      setSlug(category.slug)
                    }}
                  >
                    Cancel
                  </button>
                </div>
              </form>
            )}
          </div>
        )}
      </div>
    </main>
  )
}