import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { notificationApi } from '../api/notification'
import type { NotificationResponseDto } from '../types/notification' 
import Brand from '../components/Brand'

type FilterType = 'all' | 'unread' | 'read'

export default function NotificationsPage() {
  const [notifications, setNotifications] = useState<NotificationResponseDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage] = useState('')
  const [filter, setFilter] = useState<FilterType>('all')

  useEffect(() => {
    async function initNotifications() {
      try {
        setLoading(true)
        const data = await notificationApi.getNotifications()
        setNotifications(data)

        const hasUnread = data.some(n => !n.isRead)
        if (hasUnread) {
          await notificationApi.markNotificationsAsRead()
          setNotifications(prev => prev.map(n => ({ ...n, isRead: true })))
        }
      } catch (err: any) {
        setError(err.message || 'Failed to load notifications')
      } finally {
        setLoading(false)
      }
    }

    initNotifications()
  }, [])

  const filteredNotifications = notifications.filter(notification => {
    if (filter === 'unread') return !notification.isRead
    if (filter === 'read') return notification.isRead
    return true
  })

  if (loading) return <div className="loading">Loading notifications...</div>

  return (
    <main className="notifications-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <div className="site-header__actions">
            <Link to="/orders" className="button button--quiet">Orders</Link>
            <Link to="/main" className="button button--quiet">← Back to Catalog</Link>
          </div>
        </div>
      </header>

      <div className="notifications-container">
        <div className="notifications-header">
          <h1>Notifications</h1>
          
          <div className="notifications-filters">
            <button 
              className={`button button--small ${filter === 'all' ? 'button--dark' : 'button--quiet'}`}
              onClick={() => setFilter('all')}
            >
              All ({notifications.length})
            </button>
            <button 
              className={`button button--small ${filter === 'unread' ? 'button--dark' : 'button--quiet'}`}
              onClick={() => setFilter('unread')}
            >
              Unread ({notifications.filter(n => !n.isRead).length})
            </button>
            <button 
              className={`button button--small ${filter === 'read' ? 'button--dark' : 'button--quiet'}`}
              onClick={() => setFilter('read')}
            >
              Read ({notifications.filter(n => n.isRead).length})
            </button>
          </div>
        </div>

        {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
        {error && <p className="form-message form-message--error">{error}</p>}

        {filteredNotifications.length === 0 ? (
          <div className="empty-notifications">
            <p>No notifications found in this category.</p>
            <Link to="/main" className="button button--dark">Go to Catalog</Link>
          </div>
        ) : (
          <div className="notifications-list">
            {filteredNotifications.map((notification) => (
              <div 
                key={notification.id} 
                className={`notification-card ${notification.isRead ? 'read' : 'unread'}`}
              >
                <div className="notification-content">
                  <div className="notification-header-row">
                    <span className="notification-date">
                      {new Date(notification.createdAt).toLocaleDateString()} {new Date(notification.createdAt).toLocaleTimeString()}
                    </span>
                    {!notification.isRead && <span className="unread-dot" title="Unread"></span>}
                  </div>
                  
                  <h4 className="notification-title">{notification.title}</h4>
                  <p className="notification-text">{notification.message}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </main>
  )
}