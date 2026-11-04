import { useEffect, useMemo, useRef, useState } from 'react';
import { ImagePlus, Pause, Play, RefreshCw } from 'lucide-react';
import { renderImageScene, renderScene } from '../../api/simulations';

function usePrefersReducedMotion() {
  const [reduced, setReduced] = useState(false);
  useEffect(() => {
    const media = window.matchMedia('(prefers-reduced-motion: reduce)');
    setReduced(media.matches);
    const onChange = () => setReduced(media.matches);
    media.addEventListener('change', onChange);
    return () => media.removeEventListener('change', onChange);
  }, []);
  return reduced;
}

export default function SvgScenePlayer({ modelKey, state }) {
  const reducedMotion = usePrefersReducedMotion();
  const [scene, setScene] = useState(null);
  const [frameSvg, setFrameSvg] = useState('');
  const [image, setImage] = useState(null);
  const [imageError, setImageError] = useState('');
  const [imageLoading, setImageLoading] = useState(false);
  const [error, setError] = useState('');
  const [frame, setFrame] = useState(0);
  const [playing, setPlaying] = useState(false);
  const fetchedFrame = useRef(-1);

  const stateJson = useMemo(() => JSON.stringify(state ?? {}), [state]);

  useEffect(() => {
    if (!modelKey) return undefined;
    let cancelled = false;
    setError('');
    fetchedFrame.current = -1;
    renderScene(modelKey, JSON.parse(stateJson))
      .then((data) => { if (!cancelled) { setScene(data); setFrame(0); } })
      .catch(() => { if (!cancelled) setError('Scene preview failed to load.'); });
    return () => { cancelled = true; };
  }, [modelKey, stateJson]);

  const trajectory = scene?.result?.trajectory ?? [];
  const result = scene?.result;

  // Repaint the SVG for the selected frame (marker, bars, block position).
  useEffect(() => {
    if (!scene || trajectory.length === 0) return undefined;
    if (fetchedFrame.current === frame) return undefined;
    let cancelled = false;
    renderScene(modelKey, state, frame)
      .then((data) => {
        if (!cancelled) {
          fetchedFrame.current = frame;
          setFrameSvg(data.svg || '');
        }
      })
      .catch(() => { /* keep the previous frame visible */ });
    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [frame, scene]);

  useEffect(() => {
    if (!playing || trajectory.length < 2) return undefined;
    const timer = setInterval(() => {
      setFrame((f) => (f + 1) % trajectory.length);
    }, 600);
    return () => clearInterval(timer);
  }, [playing, trajectory.length]);

  useEffect(() => {
    if (reducedMotion) setPlaying(false);
  }, [reducedMotion]);

  if (!modelKey) return null;

  const point = trajectory[frame];
  const svg = frameSvg || scene?.svg;
  return (
    <section className="bg-white rounded-xl border border-slate-200 p-5">
      <div className="flex items-center justify-between gap-3 mb-3">
        <h3 className="font-bold text-slate-800 text-sm">Live scene</h3>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setPlaying((p) => !p)}
            disabled={trajectory.length < 2 || reducedMotion}
            className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg border border-slate-200 text-xs font-medium disabled:opacity-50"
          >
            {playing ? <Pause className="w-3.5 h-3.5" /> : <Play className="w-3.5 h-3.5" />}
            {playing ? 'Pause' : 'Play'}
          </button>
          <button
            type="button"
            onClick={() => {
              setError('');
              fetchedFrame.current = -1;
              renderScene(modelKey, state).then(setScene).catch(() => setError('Scene preview failed to load.'));
            }}
            className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg border border-slate-200 text-xs font-medium"
          >
            <RefreshCw className="w-3.5 h-3.5" /> Refresh
          </button>
          <button
            type="button"
            disabled={imageLoading}
            onClick={() => {
              setImageError('');
              setImageLoading(true);
              renderImageScene(modelKey, state)
                .then(setImage)
                .catch(() => setImageError('Image preview failed to load.'))
                .finally(() => setImageLoading(false));
            }}
            className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg border border-slate-200 text-xs font-medium disabled:opacity-50"
          >
            <ImagePlus className="w-3.5 h-3.5" /> {imageLoading ? 'Generating…' : 'Generate image'}
          </button>
        </div>
      </div>
      {imageError && <p className="text-xs text-rose-600 mb-2">{imageError}</p>}
      {(image?.imageUrl || image?.imageBase64) && (
        <img
          src={image.imageUrl || `data:image/png;base64,${image.imageBase64}`}
          alt="Generated illustration for this simulation"
          className="w-full rounded-lg border border-slate-100 mb-3"
        />
      )}
      {image && !image.imageUrl && !image.imageBase64 && (
        <p className="text-xs text-slate-500 mb-2">Image model not configured. Showing the exact SVG scene.</p>
      )}
      {error && <p className="text-xs text-rose-600 mb-2">{error}</p>}
      {!scene && !error && <p className="text-xs text-slate-500">Loading scene…</p>}
      {svg && (
        <div className="rounded-lg overflow-hidden border border-slate-100" dangerouslySetInnerHTML={{ __html: svg }} />
      )}
      {trajectory.length > 1 && point && (
        <div className="mt-3">
          <input
            type="range"
            min={0}
            max={trajectory.length - 1}
            step={1}
            value={frame}
            onChange={(e) => { setPlaying(false); setFrame(Number(e.target.value)); }}
            className="w-full accent-emerald-700"
            aria-label="Timeline frame"
          />
          <p className="text-xs text-slate-600 mt-1">
            Frame {frame + 1} of {trajectory.length}: t = {point.timeSeconds}, position = {point.positionMetres}, velocity = {point.velocityMetresPerSecond}
          </p>
        </div>
      )}
      {result?.summary && <p className="text-xs text-slate-700 mt-3">{result.summary}</p>}
    </section>
  );
}
