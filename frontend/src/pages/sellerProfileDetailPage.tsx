import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { sellerProfileApi } from '../api/seller'
import { productApi } from '../api/product' 
import type { SellerProfileResponseDto } from '../types/seller'
import type { ProductResponseDto } from '../types/product'
import Brand from '../components/Brand'

export default function SellerProfileDetailPage() {
  const { id } = useParams<{ id: string }>()

  const [seller, setSeller] = useState<SellerProfileResponseDto | null>(null)
  const [avatarUrl, setAvatarUrl] = useState<string | null>(null)
  const [products, setProducts] = useState<ProductResponseDto[]>([])
  const [productImages, setProductImages] = useState<Record<string, string>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!id) return

    async function loadSellerData() {
      try {
        setLoading(true)
        setError('')

        const [profileData, img, productsData] = await Promise.all([
          sellerProfileApi.getUserSellerProfile(id!),
          sellerProfileApi.getSellerProfileImage(id!).catch(() => null),
          productApi.getProductsOfSeller(id!).catch(() => []),
        ])

        setSeller(profileData)
        setAvatarUrl(img)
        setProducts(productsData)

        if (productsData.length > 0) {
          const imagePromises = productsData.map(async (product) => {
            try {
              const productImgUrl = await productApi.getProductImage(product.id)
              return { id: product.id, url: productImgUrl }
            } catch {
              return { id: product.id, url: null }
            }
          })

          const imagesResults = await Promise.all(imagePromises)
          const imagesMap: Record<string, string> = {}
          imagesResults.forEach((item) => {
            if (item.url) {
              imagesMap[item.id] = item.url
            }
          })
          setProductImages(imagesMap)
        }

      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to load seller profile')
      } finally {
        setLoading(false)
      }
    }

    loadSellerData()
  }, [id])

  if (loading) return <div className="loading">Loading seller profile...</div>
  if (error) return <div className="error-container"><h2>Error</h2><p>{error}</p><Link to="/main">Back to catalog</Link></div>
  if (!seller) return <div className="loading">Seller not found</div>

  return (
    <main className="seller-profile-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/main" className="button button--quiet">← Back to Catalog</Link>
        </div>
      </header>

      <div className="seller-profile-container">
        <div className="seller-hero">
          <div className="seller-hero__avatar-wrapper">
            {avatarUrl ? (
              <img src={avatarUrl} alt={seller.storeName} className="seller-hero__avatar" />
            ) : (
              <div className="seller-hero__avatar-placeholder">🏪</div>
            )}
          </div>

          <div className="seller-hero__info">
            <div className="seller-hero__title-row">
              <h1>{seller.storeName}</h1>
              <span className={`seller-status-badge seller-status--${String(seller.status).toLowerCase()}`}>
                {seller.status}
              </span>
            </div>
            <p className="seller-hero__desc">
              {seller.description || 'This store has no description yet.'}
            </p>
            <span className="seller-hero__date">
              Member since: {new Date(seller.createdAt).toLocaleDateString()}
            </span>
          </div>
        </div>

        <section className="seller-products-section">
          <h2>Products by {seller.storeName} ({products.length})</h2>

          {products.length === 0 ? (
            <p className="no-products">This seller hasn't added any products yet.</p>
          ) : (
            <div className="products-grid">
              {products.map((product) => {
                const productImg = productImages[product.id]
                return (
                  <Link to={`/product/${product.id}`} key={product.id} className="product-card">
                    <div className="product-card__image-wrapper">
                      {productImg ? (
                        <img src={productImg} alt={product.name} className="product-card__image" />
                      ) : (
                        <div className="product-card__placeholder">No image</div>
                      )}
                    </div>
                    <div className="product-card__info">
                      <h3>{product.name}</h3>
                      <p className="product-card__price">${product.price}</p>
                    </div>
                  </Link>
                )
              })}
            </div>
          )}
        </section>
      </div>
    </main>
  )
}