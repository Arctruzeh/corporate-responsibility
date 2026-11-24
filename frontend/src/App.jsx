import { Component, useState, useEffect, useCallback, useRef } from 'react'
import './all.css'
import BackToPortfolio from './BackToPortfolio'
import { BiRefresh } from 'react-icons/bi'

// Error Boundary Component
class ErrorBoundary extends Component {
    constructor(props) {
        super(props)
        this.state = { hasError: false }
    }

    static getDerivedStateFromError() {
        return { hasError: true }
    }

    render() {
        if (this.state.hasError) {
            return <div className="error-message">Something went wrong. Please try again.</div>
        }
        return this.props.children
    }
}

const PHRASE_FADE_DURATION = 200; // match CSS .phrase transition duration
const CROSSFADE_SETTLE_DURATION = 300; // remaining time for 500ms background crossfade to settle
const BUTTON_TEXTS = [
    "Synergize Content",
    "Ideate Solutions",
    "Leverage New Content",
    "Disrupt Status Quo",
    "Pivot Strategy",
    "Optimize Output",
    "Revolutionize Vision",
    "Accelerate Growth",
    "Transform Paradigms",
    "Maximize Potential"
];

const API_BASE_URL = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '');
const RESPONSIBILITY_API_URL = `${API_BASE_URL}/api/responsibility`;

const preloadImage = (url) => {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve(url);
        img.onerror = () => reject(new Error(`Failed to load image: ${url}`));
        img.src = url;
    });
};

const fetchNewData = async (signal) => {
    try {
        const response = await fetch(RESPONSIBILITY_API_URL, { signal });
        if (!response.ok) {
            throw new Error(`Responsibility API failed with status ${response.status}`);
        }

        const data = await response.json();
        if (!data?.phrase) {
            throw new Error('Responsibility API returned an empty phrase');
        }

        let imageUrl = data.imageUrl ?? '';
        if (imageUrl) {
            if (signal?.aborted) {
                return null;
            }
            try {
                await preloadImage(imageUrl);
            } catch (imageError) {
                console.error("Error preloading background image:", imageError);
                imageUrl = '';
            }
        }

        if (signal?.aborted) {
            return null;
        }

        return {
            phrase: data.phrase,
            imageUrl
        };
    } catch (error) {
        if (error.name === 'AbortError' || signal?.aborted) {
            return null;
        }
        console.error("Error fetching corporate responsibility:", error);
        return { phrase: "Error loading content", imageUrl: "" };
    }
};

const App = () => {
    const [items, setItems] = useState({ phrase: '' });
    const [bgLayers, setBgLayers] = useState(['', '']);
    const [activeLayer, setActiveLayer] = useState(0);
    const activeLayerRef = useRef(0);
    const [currentButtonTextIndex, setCurrentButtonTextIndex] = useState(0);
    const [isPhraseFading, setIsPhraseFading] = useState(false);
    const [isInitialLoading, setIsInitialLoading] = useState(true);
    const [isTransitioning, setIsTransitioning] = useState(false);
    const isTransitioningRef = useRef(false);

    // Buffer for next content
    const [nextContent, setNextContent] = useState(null);

    const prefetchNext = useCallback(async () => {
        const data = await fetchNewData();
        if (!data) return;
        setNextContent(data);
    }, []);

    const theFetch = useCallback(async () => {
        if (isInitialLoading || isTransitioningRef.current) {
            return;
        }

        isTransitioningRef.current = true;
        setIsTransitioning(true);

        try {
            // Fade out only the phrase text while keeping the overlay steady
            setIsPhraseFading(true);

            const [data] = await Promise.all([
                nextContent ? Promise.resolve(nextContent) : fetchNewData(),
                new Promise(resolve => setTimeout(resolve, PHRASE_FADE_DURATION))
            ]);

            const nextImageUrl = data?.imageUrl ?? '';
            const nextLayer = activeLayerRef.current === 0 ? 1 : 0;

            setBgLayers(prev =>
                nextLayer === 0
                    ? [nextImageUrl, prev[1]]
                    : [prev[0], nextImageUrl]
            );
            activeLayerRef.current = nextLayer;
            setActiveLayer(nextLayer);

            setItems({ phrase: data?.phrase ?? "Error loading content" });
            setNextContent(null);
            setCurrentButtonTextIndex(prev => (prev + 1) % BUTTON_TEXTS.length);
            setIsPhraseFading(false);

            // Start prefetching next content
            prefetchNext();

            // Hold transition lock until background crossfade completes
            await new Promise(resolve => setTimeout(resolve, CROSSFADE_SETTLE_DURATION));
        } finally {
            setIsPhraseFading(false);
            isTransitioningRef.current = false;
            setIsTransitioning(false);
        }
    }, [isInitialLoading, nextContent, prefetchNext]);

    // Initial mount
    useEffect(() => {
        const controller = new AbortController();

        const loadInitial = async () => {
            const data = await fetchNewData(controller.signal);
            if (!data || controller.signal.aborted) return;

            if (data.imageUrl) {
                setBgLayers(['', data.imageUrl]);
                activeLayerRef.current = 1;
                setActiveLayer(1);
            }

            setItems({ phrase: data.phrase });
            setIsInitialLoading(false);
            prefetchNext();
        };

        loadInitial();

        return () => {
            controller.abort();
        };
    }, [prefetchNext]);

    return (
        <ErrorBoundary>
            <div className="my-style">
                <div
                    className="bg-layer bg-layer--base"
                    style={bgLayers[0] ? { backgroundImage: `url(${bgLayers[0]})` } : undefined}
                />
                <div
                    className={`bg-layer bg-layer--top ${activeLayer === 1 ? 'is-visible' : ''}`}
                    style={bgLayers[1] ? { backgroundImage: `url(${bgLayers[1]})` } : undefined}
                />
                <div className="my-style-2">
                    <div
                        className={`phrase ${isPhraseFading ? 'phrase--fading' : ''}`}
                        aria-live="polite"
                    >
                        {isInitialLoading ? (
                            <div className="phrase-loader" role="status" aria-live="polite">
                                <span className="sr-only">Loading corporate synergy...</span>
                                <span className="loader-bar loader-bar--primary" aria-hidden="true" />
                                <span className="loader-bar loader-bar--secondary" aria-hidden="true" />
                            </div>
                        ) : (
                            <span className="phrase-text">{items.phrase}</span>
                        )}
                    </div>
                    <button
                        onClick={theFetch}
                        disabled={isInitialLoading || isTransitioning}
                        className="refresh-button"
                    >
                        <BiRefresh className="button-icon" aria-hidden="true" />
                        <span className="button-label">
                            {BUTTON_TEXTS[currentButtonTextIndex]}
                        </span>
                    </button>
                    <BackToPortfolio />
                </div>
            </div>
        </ErrorBoundary>
    )
}

export default App
