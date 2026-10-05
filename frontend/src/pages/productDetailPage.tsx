import { useEffect, useState, type FormEvent } from 'react'
import { useParams, Link } from 'react-router-dom'
import { productApi } from '../api/product'
import { reviewApi } from '../api/review'
import { cartItemApi } from '../api/cart' 
import { sellerProfileApi } from '../api/seller'
import type { ProductResponseDto } from '../types/product'
import type { ReviewResponseDto } from '../types/review'
import type { SellerPreviewResponseDto } from '../types/seller'
import Brand from '../components/Brand'

export default function ProductDetailPage() {
  const { id } = useParams<{ id: string }>()
  
  const [product, setProduct] = useState<ProductResponseDto | null>(null)
  const [imageUrl, setImageUrl] = useState<string | null>(null)
  const [reviews, setReviews] = useState<ReviewResponseDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  const [sellerPreview, setSellerPreview] = useState<SellerPreviewResponseDto | null>(null)
  const [sellerImageUrl, setSellerImageUrl] = useState<string | null>(null)

  const [quantity, setQuantity] = useState(1)
  const [addingToCart, setAddingToCart] = useState(false)

  const [rating, setRating] = useState(5)
  const [comment, setComment] = useState('')
  const [submittingReview, setSubmittingReview] = useState(false)

  useEffect(() => {
    if (!id) return

    async function loadProductData() {
      try {
        setLoading(true)
        const [prodData, reviewsData, imgUrl] = await Promise.all([
          productApi.getProductById(id!),
          reviewApi.getReviewsByProduct(id!),
          productApi.getProductImage(id!).catch(() => null)
        ])
        
        setProduct(prodData)
        setReviews(reviewsData)
        setImageUrl(imgUrl)

        const sellerProfileId = (prodData as any).sellerProfileId

        if (sellerProfileId) {
          try {
            const [previewData, avatarUrl] = await Promise.all([
              sellerProfileApi.getSellerProfilePreview(sellerProfileId),
              sellerProfileApi.getSellerProfileImage(sellerProfileId).catch(() => null)
            ])
            setSellerPreview(previewData)
            setSellerImageUrl(avatarUrl)
          } catch (err) {
          }
        }

      } catch (err: any) {
        setError(err.message || 'Failed to load product details')
      } finally {
        setLoading(false)
      }
    }

    loadProductData()
  }, [id])

  async function handleAddToCart() {
    if (!id) return
    try {
      setError('')
      setAddingToCart(true)
      await cartItemApi.createCartItem({
        productId: id,
        quantity: quantity
      })
      setSuccessMessage('Product successfully added to cart!')
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to add item to cart')
    } finally {
      setAddingToCart(false)
    }
  }

  async function handleAddReview(e: FormEvent) {
    e.preventDefault()
    if (!id || !comment.trim()) return

    try {
      setSubmittingReview(true)
      setError('')
      const newReview = await reviewApi.createReview(id, { rating, comment })
      setReviews([newReview, ...reviews])
      setComment('')
      setRating(5)
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to add review')
    } finally {
      setSubmittingReview(false)
    }
  }

  async function handleDeleteReview(reviewId: string) {
    try {
      await reviewApi.deleteReview(reviewId)
      setReviews(reviews.filter(r => r.id !== reviewId))
    } catch (err: any) {
      setError(err.message || 'Failed to delete review')
    }
  }

  if (loading) return <div className="loading">Loading product...</div>
  if (error && !product) return <div className="error-container"><h2>Error</h2><p>{error}</p><Link to="/main">Back to catalog</Link></div>
  if (!product) return <div className="loading">Product not found</div>

  const sellerProfileId = (product as any).sellerProfileId

  return (
    <main className="product-detail-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <Link to="/main" className="button button--quiet">← Back to Catalog</Link>
        </div>
      </header>

      <div className="product-detail-container">
        <div className="product-main-info">
          <div className="product-detail-image-wrapper">
            {imageUrl ? (
              <img src={imageUrl} alt={product.name} className="product-detail-image" />
            ) : (
              <div className="product-detail-placeholder">No image available</div>
            )}
          </div>

          <div className="product-detail-content">
            <h1>{product.name}</h1>
            <p className="product-detail-desc">{product.description}</p>
            
            <div className="product-detail-meta">
              <span className="product-detail-price">${product.price}</span>
              <span className="product-detail-stock">In stock: {product.stock}</span>
            </div>

            {sellerPreview && sellerProfileId && (
              <Link to={`/seller/${sellerProfileId}`} className="seller-card-preview">
                <div className="seller-avatar-wrapper">
                  {sellerImageUrl ? (
                    <img src={sellerImageUrl} alt={sellerPreview.storeName} className="seller-avatar" />
                  ) : (
                    <div className="seller-avatar-placeholder">🏪</div>
                  )}
                </div>
                <div className="seller-preview-info">
                  <span className="seller-label">Sold by</span>
                  <h4 className="seller-store-name">{sellerPreview.storeName}</h4>
                  {sellerPreview.description && (
                    <p className="seller-preview-desc">{sellerPreview.description}</p>
                  )}
                </div>
              </Link>
            )}

            {successMessage && <p className="form-message form-message--success">{successMessage}</p>}
            {error && <p className="form-message form-message--error">{error}</p>}

            <div className="cart-actions">
              <label className="form-field-inline">
                <span>Quantity:</span>
                <input 
                  type="number" 
                  min={1} 
                  max={product.stock} 
                  value={quantity} 
                  onChange={(e) => setQuantity(Number(e.target.value))}
                />
              </label>

              <button 
                className="button button--dark button--large" 
                onClick={handleAddToCart}
                disabled={addingToCart}
              >
                {addingToCart ? 'Adding...' : 'Add to Cart'}
              </button>
            </div>
          </div>
        </div>

        <section className="reviews-section">
          <h2>Customer Reviews ({reviews.length})</h2>

          <form className="review-form" onSubmit={handleAddReview}>
            <h3>Leave a Review</h3>
            <label className="form-field">
              <span>Rating (1 - 5)</span>
              <select value={rating} onChange={(e) => setRating(Number(e.target.value))}>
                <option value={5}>5 - Excellent</option>
                <option value={4}>4 - Good</option>
                <option value={3}>3 - Average</option>
                <option value={2}>2 - Poor</option>
                <option value={1}>1 - Terrible</option>
              </select>
            </label>

            <label className="form-field">
              <span>Comment</span>
              <textarea 
                rows={3} 
                placeholder="Share your thoughts about the product..." 
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                required
              />
            </label>

            <button type="submit" className="button button--dark" disabled={submittingReview}>
              {submittingReview ? 'Submitting...' : 'Post Review'}
            </button>
          </form>

          <div className="reviews-list">
            {reviews.length === 0 ? (
              <p>No reviews yet. Be the first to review!</p>
            ) : (
              reviews.map((rev) => (
                <div key={rev.id} className="review-card">
                  <div className="review-header">
                    <span className="review-rating">⭐ {rev.rating} / 5</span>
                    <span className="review-date">{new Date(rev.createdAt).toLocaleDateString()}</span>
                  </div>
                  <p className="review-comment">{rev.comment}</p>
                  <button 
                    type="button"
                    className="button button--quiet button--small text-danger" 
                    onClick={() => handleDeleteReview(rev.id)}
                  >
                    Delete
                  </button>
                </div>
              ))
            )}
          </div>
        </section>
      </div>
    </main>
  )
}