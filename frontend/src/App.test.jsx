import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import App from './App';

describe('App', () => {
    const originalFetch = global.fetch;
    const originalImage = global.Image;

    beforeEach(() => {
        global.Image = class MockImage {
            set src(value) {
                this._src = value;
                queueMicrotask(() => {
                    this.onload?.();
                });
            }
        };
    });

    afterEach(() => {
        global.fetch = originalFetch;
        global.Image = originalImage;
        vi.restoreAllMocks();
    });

    it('shows initial loading state with disabled button until content loads', async () => {
        let resolveResponsibility;
        global.fetch = vi.fn(() => {
            return new Promise((resolve) => {
                resolveResponsibility = resolve;
            });
        });

        const { container } = render(<App />);

        expect(screen.getByRole('status')).toHaveTextContent(
            'Loading corporate synergy...'
        );
        expect(container.querySelectorAll('.loader-bar')).toHaveLength(2);
        const refreshButton = screen.getByRole('button', {
            name: /synergize content/i
        });
        expect(refreshButton).toBeDisabled();

        resolveResponsibility({
            ok: true,
            json: () =>
                Promise.resolve({
                    phrase: 'Synergize scalable deliverables',
                    imageUrl: 'https://example.com/office.jpg',
                    searchQuery: 'office'
                })
        });

        expect(
            await screen.findByText('Synergize scalable deliverables')
        ).toBeInTheDocument();
        expect(container.querySelectorAll('.loader-bar')).toHaveLength(0);
        expect(refreshButton).toBeEnabled();
        expect(
            screen.getByRole('link', { name: /back to portfolio/i })
        ).toHaveAttribute('href', 'https://ajlato.com/projects/corporate-responsibility');
    });

    it('crossfades background layers and guards against rapid clicks during transition', async () => {
        let callCount = 0;
        global.fetch = vi.fn(() => {
            callCount += 1;
            const entries = [
                {
                    phrase: 'Synergize scalable deliverables',
                    imageUrl: 'https://example.com/first.jpg',
                    searchQuery: 'office'
                },
                {
                    phrase: 'Leverage proactive paradigms',
                    imageUrl: 'https://example.com/second.jpg',
                    searchQuery: 'meeting'
                },
                {
                    phrase: 'Monetize seamless verticals',
                    imageUrl: 'https://example.com/third.jpg',
                    searchQuery: 'corporate'
                }
            ];
            const payload = entries[callCount - 1] ?? entries[entries.length - 1];
            return Promise.resolve({
                ok: true,
                json: () => Promise.resolve(payload)
            });
        });

        const { container } = render(<App />);

        expect(
            await screen.findByText('Synergize scalable deliverables')
        ).toBeInTheDocument();

        const baseLayer = container.querySelector('.bg-layer--base');
        const topLayer = container.querySelector('.bg-layer--top');
        expect(container.querySelector('.my-style')).not.toHaveClass('fade-out');
        expect(topLayer).toHaveClass('is-visible');
        expect(topLayer).toHaveStyle({
            backgroundImage: 'url(https://example.com/first.jpg)'
        });

        const refreshButton = screen.getByRole('button', {
            name: /synergize content/i
        });

        await waitFor(() => {
            expect(callCount).toBe(2);
        });

        fireEvent.click(refreshButton);
        expect(refreshButton).toBeDisabled();

        // Second rapid click during transition should be ignored
        fireEvent.click(refreshButton);

        expect(
            await screen.findByText('Leverage proactive paradigms')
        ).toBeInTheDocument();

        expect(container.querySelector('.my-style')).not.toHaveClass('fade-out');
        expect(topLayer).not.toHaveClass('is-visible');
        expect(baseLayer).toHaveStyle({
            backgroundImage: 'url(https://example.com/second.jpg)'
        });

        await waitFor(() => {
            expect(
                screen.getByRole('button', { name: /ideate solutions/i })
            ).toBeEnabled();
        });
    });

    it('still displays generated phrase when backend returns an empty imageUrl', async () => {
        global.fetch = vi.fn(() =>
            Promise.resolve({
                ok: true,
                json: () =>
                    Promise.resolve({
                        phrase: 'Empower holistic ecosystems',
                        imageUrl: '',
                        searchQuery: 'office'
                    })
            })
        );

        const { container } = render(<App />);

        expect(
            await screen.findByText('Empower holistic ecosystems')
        ).toBeInTheDocument();
        expect(screen.queryByText('Error loading content')).not.toBeInTheDocument();
        expect(container.querySelector('.bg-layer--base')?.style.backgroundImage).toBe('');
        expect(container.querySelector('.bg-layer--top')?.style.backgroundImage).toBe('');
    });

    it('still displays generated phrase when image CDN preload fails', async () => {
        vi.spyOn(console, 'error').mockImplementation(() => {});
        global.Image = class FailingMockImage {
            set src(value) {
                this._src = value;
                queueMicrotask(() => {
                    this.onerror?.();
                });
            }
        };

        global.fetch = vi.fn(() =>
            Promise.resolve({
                ok: true,
                json: () =>
                    Promise.resolve({
                        phrase: 'Empower holistic ecosystems',
                        imageUrl: 'https://example.com/broken.jpg',
                        searchQuery: 'office'
                    })
            })
        );

        const { container } = render(<App />);

        expect(
            await screen.findByText('Empower holistic ecosystems')
        ).toBeInTheDocument();
        expect(container.querySelector('.bg-layer--base')?.style.backgroundImage).toBe('');
        expect(container.querySelector('.bg-layer--top')?.style.backgroundImage).toBe('');
    });

    it('shows fallback text when responsibility API request fails', async () => {
        vi.spyOn(console, 'error').mockImplementation(() => {});

        global.fetch = vi.fn(() =>
            Promise.resolve({
                ok: false,
                status: 500
            })
        );

        render(<App />);

        expect(
            await screen.findByText('Error loading content')
        ).toBeInTheDocument();
    });
});
