import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { sellerProfileApi } from '../api/seller'
import type { 
  SellerProfileResponseDto, 
  SellerProfileCreateDto, 
  SellerProfileUpdateDto 
} from '../types/seller'
import { SellerStatus } from '../types/seller'
import Brand from '../components/Brand'

export default function SellerProfilePage() {
  const [profile, setProfile] = useState<SellerProfileResponseDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  // Поля формы создания
  const [storeName, setStoreName] = useState('')
  const [description, setDescription] = useState('')

  // Поля формы редактирования
  const [editStoreName, setEditStoreName] = useState('')
  const [editDescription, setEditDescription] = useState('')
  const [isEditing, setIsEditing] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  // Проверяем наличие профиля при загрузке страницы
  useEffect(() => {
    async function fetchProfile() {
      try {
        setLoading(true)
        const data = await sellerProfileApi.getCurrentUserSellerProfile()
        setProfile(data)
        setEditStoreName(data.storeName)
        setEditDescription(data.description || '')
      } catch (err: any) {
        // Если профиль не найден (обычно 404), оставляем profile = null для отображения формы создания
        if (err.response?.status !== 404) {
          setError(err.message || 'Failed to load seller profile')
        }
      } finally {
        setLoading(false)
      }
    }

    fetchProfile()
  }, [])

  // Обработка создания профиля
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

  // Обработка обновления профиля
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
      
      // Обновляем локальное состояние
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

  // Функция для отображения статуса продавца текстом
  function renderStatus(status: SellerStatus) {
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

        {/* Если профиля нет — показываем форму создания */}
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
          /* Если профиль есть — показываем информацию или форму редактирования */
          <div className="profile-card">
            <div className="profile-header-info">
              <div className="store-title-status">
                <h2>{profile.storeName}</h2>
                {renderStatus(profile.status)}
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
        )}
      </div>
    </main>
  )
}