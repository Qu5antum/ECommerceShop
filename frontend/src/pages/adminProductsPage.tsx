import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { productApi } from '../api/product'
import { userApi } from '../api/user'
import type { ProductResponseDto } from '../types/product'
import type { UserResponseDto } from '../types/user'
import { UserRole } from '../types/user'
import Brand from '../components/Brand'
import '../style/adminProductsPage.css'

export default function AdminProductsPage() {
  const navigate = useNavigate()
  const [currentUser, setCurrentUser] = useState<UserResponseDto | null>(null)
  const [products, setProducts] = useState<ProductResponseDto[]>([])
  const [productImages, setProductImages] = useState<Record<string, string>>({})
  const [searchQuery, setSearchQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    async function initAdminProducts() {
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

        await fetchProducts()
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to initialize products page')
        setLoading(false)
      }
    }

    initAdminProducts()
  }, [])

  async function fetchProducts(query?: string) {
    try {
      setLoading(true)
      setError('')

      let data: ProductResponseDto[] = []
      if (query && query.trim() !== '') {
        data = await productApi.searchProduct(query.trim())
      } else {
        data = await productApi.getProducts()
      }

      setProducts(data)

      if (data.length > 0) {
        const imagePromises = data.map(async (product: ProductResponseDto) => {
          try {
            const url = await productApi.getProductImage(product.id)
            return { id: product.id, url }
          } catch {
            return { id: product.id, url: null }
          }
        })

        const imagesResults = await Promise.all(imagePromises)
        const imagesMap: Record<string, string> = {}
        imagesResults.forEach((item) => {
          if (item.url) imagesMap[item.id] = item.url
        })
        setProductImages(imagesMap)
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load products')
    } finally {
      setLoading(false)
    }
  }

  function handleSearchSubmit(e: React.FormEvent) {
    e.preventDefault()
    fetchProducts(searchQuery)
  }

  if (loading && products.length === 0) return <div className="loading">Loading products...</div>

  if (error && error.includes('Access Denied')) {
    return (
      <main className="admin-products-page">
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
    <main className="admin-products-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/admin/users" className="button button--quiet">← Back to Admin Panel</Link>
        </div>
      </header>

      <div className="admin-container">
        <div className="admin-header-row">
          <h1>All Products Management</h1>
          <span className="admin-role-indicator">
            Logged in as: <strong>{currentUser?.userName}</strong>
          </span>
        </div>

        <form onSubmit={handleSearchSubmit} className="search-filter-row" style={{ display: 'flex', gap: '10px', marginBottom: '20px' }}>
          <input 
            type="text" 
            placeholder="Search product by name..." 
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            style={{ flex: 1, padding: '10px', borderRadius: '8px', border: '1px solid #e4e4e7' }}
          />
          <button type="submit" className="button button--dark">Search</button>
          {searchQuery && (
            <button 
              type="button" 
              className="button button--quiet" 
              onClick={() => { setSearchQuery(''); fetchProducts(''); }}
            >
              Reset
            </button>
          )}
        </form>

        {error && <p className="form-message form-message--error">{error}</p>}

        {products.length === 0 ? (
          <div className="no-products-card">
            <p>No products found.</p>
          </div>
        ) : (
          <div className="admin-products-grid">
            {products.map((product) => {
              const imgUrl = productImages[product.id]
              return (
                <div 
                  key={product.id} 
                  className="admin-product-card"
                  onClick={() => navigate(`/admin/products/${product.id}`)}
                  style={{ cursor: 'pointer' }}
                >
                  <div className="product-card-image-box">
                    {imgUrl ? (
                      <img src={imgUrl} alt={product.name} />
                    ) : (
                      <span className="no-image-placeholder">No Image</span>
                    )}
                    {product.stock === 0 && (
                      <span className="out-of-stock-badge">Out of Stock</span>
                    )}
                  </div>
                  <div className="product-card-body">
                    <h3>{product.name}</h3>
                    <p className="product-sku">SKU: {product.sku}</p>
                    <p className="product-desc">{product.description}</p>
                    <div className="product-meta-row">
                      <span className="product-price">${product.price.toFixed(2)}</span>
                      <span className={`product-stock ${product.stock === 0 ? 'text-danger' : ''}`}>
                        Stock: {product.stock}
                      </span>
                    </div>
                  </div>
                  <div className="product-card-actions" style={{ padding: '0 16px 16px' }}>
                    <Link 
                      to={`/admin/products/${product.id}`} 
                      className="button button--quiet"
                      onClick={(e) => e.stopPropagation()}
                    >
                      Manage Product →
                    </Link>
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </div>
    </main>
  )
}