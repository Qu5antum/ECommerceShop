import Brand from '../components/Brand'

export default function HomePage() {
  return (
    <main className="home-page">
      <header className="site-header">
        <div className="site-header__inner">
          <Brand />
          <nav className="site-nav" aria-label="Main navigation">
            <a href="#about">About store</a>
            <a href="#benefits">Benefits</a>
          </nav>
          <div className="site-header__actions">
            <a className="button button--quiet" href="/login">
              Log in
            </a>
            <a className="button button--dark" href="/register">
              Sign up
            </a>
          </div>
        </div>
      </header>

      <section className="hero" id="about">
        <div className="hero__copy">
          <span className="eyebrow">
            <span className="eyebrow__dot" />
            A place for pleasant finds
          </span>
          <h1>
            Shopping
            <br />
            that <span>delights.</span>
          </h1>
          <p>
            Everything you want to find — all in one cozy store. Create an account to start your
            journey with Lavka.
          </p>
          <div className="hero__actions">
            <a className="button button--dark button--large" href="/register">
              Start shopping <span aria-hidden="true">↗</span>
            </a>
            <a className="hero__login" href="/login">
              Already have an account? <span>Log in</span>
            </a>
          </div>
          <div className="hero__note">
            <span className="hero__note-icon" aria-hidden="true">
              ✳
            </span>
            <span>Simple registration — and you are all set</span>
          </div>
        </div>

        <div className="hero-art" aria-label="Gift box illustration" role="img">
          <div className="hero-art__sun" />
          <div className="hero-art__spark hero-art__spark--one">✳</div>
          <div className="hero-art__spark hero-art__spark--two">✦</div>
          <div className="hero-art__card">
            <span className="hero-art__card-label">small joy</span>
            <div className="hero-art__gift">
              <div className="hero-art__bow hero-art__bow--left" />
              <div className="hero-art__bow hero-art__bow--right" />
              <div className="hero-art__lid" />
              <div className="hero-art__box">
                <span />
              </div>
            </div>
            <span className="hero-art__card-caption">found for everyone</span>
          </div>
          <div className="hero-art__badge">
            <span>♡</span>
            <span>choose with pleasure</span>
          </div>
          <div className="hero-art__dots" />
        </div>
      </section>

      <section className="benefits" id="benefits">
        <div className="benefits__intro">
          <span className="eyebrow">Keep it simple</span>
          <h2>A great choice starts here</h2>
        </div>
        <div className="benefit">
          <span className="benefit__number">01</span>
          <h3>Create an account</h3>
          <p>Registration takes just a couple of minutes.</p>
        </div>
        <div className="benefit">
          <span className="benefit__number">02</span>
          <h3>Find what's yours</h3>
          <p>Discover shopping all in one place.</p>
        </div>
        <div className="benefit">
          <span className="benefit__number">03</span>
          <h3>Shop with joy</h3>
          <p>Lavka is always there when you need it.</p>
        </div>
      </section>

      <footer className="site-footer">
        <Brand />
        <span>Good finds — every day.</span>
        <a href="/register">
          Join us <span aria-hidden="true">↗</span>
        </a>
      </footer>
    </main>
  )
}